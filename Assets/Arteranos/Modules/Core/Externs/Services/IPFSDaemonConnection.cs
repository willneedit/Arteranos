/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Arteranos.Core.Cryptography;
using Ipfs;
using Ipfs.Cryptography.Proto;
using Ipfs.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace Arteranos.Services
{
    public static class IPFSDaemonConnection
    {
        // API ports to avoid
        public static readonly HashSet<int> bannedAPIPorts = new() { 5001, 8080 };

        public enum Status
        {
            OK = 0,
            NoDaemonExecutable,
            NoRepository,
            CommandFailed,
            DeadDaemon,
            PortSquatter
        }

        public static IpfsClientEx Ipfs { get; private set; } = null;
        public static Peer Self { get; private set; } = null;
        public static SignKey ServerKeyPair { get; private set; } = null;
        public static string RepoDir { get; private set; } = null;

        private static string _IPFSExe = null;
        private static bool? _IPFSAccessible = null;
        private static bool? _RepoExists = null;
        private static bool? _DaemonRunning = null;
        private static int? APIPort = null;

        private static ProcessStartInfo BuildDaemonCommand(string arguments, bool includeDefaultOptions = true)
        {
            List<string> sb = new();

            if (includeDefaultOptions)
            {
                // Just set up the suitable RepoDir, don't care if it actually exists or not.
                CheckRepository(false);

                sb.Add($"--repo-dir={RepoDir}");
            }

            if (arguments != null)
                sb.Add(arguments);

            string argLine = string.Join(' ', sb);

            return new()
            {
                FileName = _IPFSExe,
                Arguments = argLine,
                UseShellExecute = false,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                CreateNoWindow = true,
            };
        }

        private static Status RunDaemonCommand(ProcessStartInfo psi, bool synced)
        {
            if(IPFSAccessible() != Status.OK) return IPFSAccessible();

            try
            {
                Process process = Process.Start(psi);

                if(synced) process.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Status.CommandFailed;
            }

            return Status.OK;
        }

        public static Status IPFSAccessible()
        {
            // Got it earlier?
            if (_IPFSAccessible != null) 
                return _IPFSAccessible.Value ? Status.OK : Status.NoDaemonExecutable;

            _IPFSExe = "ipfs.exe";

            // Just try 'ipfs.exe help'
            ProcessStartInfo psi = BuildDaemonCommand("help", false);

            try
            {
                Process process = Process.Start(psi);

                _IPFSAccessible = true;
                return Status.OK;
            }
            catch
            {
                _IPFSAccessible = false;
                return Status.NoDaemonExecutable;
            }
        }

        public static Status CheckRepository(bool reinitAllowed)
        {
            if (_RepoExists ?? false) return Status.OK;
              
            RepoDir = $"{ConfigUtils.persistentDataPath}/.ipfs";

            try
            {
                _ = IpfsClientEx.ReadDaemonPrivateKey(RepoDir);

                _RepoExists = true;
                return Status.OK;
            }
            catch
            {
                if(!reinitAllowed)
                {
                    // Just checking, and it isn't there. So, stop right now.
                    _RepoExists = false;
                    return Status.NoRepository;
                }
            }

            ProcessStartInfo psi = BuildDaemonCommand("init");
            if(RunDaemonCommand(psi, true) != Status.OK)
            {
                // Init failed!
                _RepoExists = false;
                return Status.NoRepository;
            }

            // Daemon says we're successful, but we have to verify.
            _RepoExists = null;
            return CheckRepository(false);
        }

        public static Status StartDaemon(bool forceRestart)
        {
            if (_DaemonRunning ?? false && !forceRestart) return Status.OK;
            if (forceRestart) StopDaemon();

            ProcessStartInfo psi = BuildDaemonCommand("daemon --enable-pubsub-experiment");
            Status res = RunDaemonCommand(psi, false);
            _DaemonRunning = res == Status.OK;
            return res;
        }

        public static Status StopDaemon()
        {
            ProcessStartInfo psi = BuildDaemonCommand("shutdown");
            _DaemonRunning = null;
            return RunDaemonCommand(psi, true);
        }

        public static int GetAPIPort()
        {
            if (APIPort != null) return APIPort.Value;

            if (CheckRepository(false) != Status.OK) return -1;
            int port = -1;

            try
            {
                MultiAddress apiAddr = IpfsClientEx.ReadDaemonAPIAddress(RepoDir);
                foreach (NetworkProtocol protocol in apiAddr.Protocols)
                    if (protocol.Code == 6)
                    {
                        port = int.Parse(protocol.Value);
                        break;
                    }
            }
            catch { }

            APIPort = port;
            return port;
        }

        public static async Task<Status> CheckAPIConnection(int attempts, bool verify = true)
        {
            int port = GetAPIPort();

            if (port < 0) return Status.NoRepository;

            IpfsClientEx ipfs = new($"http://localhost:{port}");

            Status status = Status.DeadDaemon;
            for(int i = 0;  i < attempts; i++)
            {
                try
                {
                    _ = await ipfs.Config.GetAsync();
                    status = Status.OK;
                    break;
                }
                catch // (Exception es) 
                {
                    // Debug.LogException(es);
                }

                await Task.Delay(1000);
            }

            if (status != Status.OK || !verify) return status;

            try
            {
                PrivateKey pk = IpfsClientEx.ReadDaemonPrivateKey(RepoDir);

                await ipfs.VerifyDaemonAsync(pk);

                ServerKeyPair = SignKey.ImportPrivateKey(pk);
                Ipfs = ipfs;

                Self = await ipfs.IdAsync();
            }
            catch // (Exception ex)
            {
                // Debug.LogError(ex);
                return Status.PortSquatter;
            }

            return Status.OK;
        }

        public static async Task<Status> EvadePortSquatters(int attempts)
        {
            Status result;

            for (int i = 0; i < attempts; i++)
            {
                result = await CheckAPIConnection(5, true);

                if (result != Status.PortSquatter) return result;

                Debug.Log($"API port squatting detected");

                StopDaemon();
                APIPort = null;

                await Task.Delay(5000);

                int newPort;
                Random rnd = new();
                
                do
                {
                    newPort = rnd.Next(5001, 49999);
                } while(bannedAPIPorts.Contains(newPort));

                string reconfigureCmd = $"config Addresses.API /ip4/127.0.0.1/tcp/{newPort}";

                ProcessStartInfo psi = BuildDaemonCommand(reconfigureCmd);
                result = RunDaemonCommand(psi, true);

                // Additionally, remove the Web Gateway port.
                psi = BuildDaemonCommand("config --json Addresses.Gateway []");
                RunDaemonCommand(psi, true);

                if(result != Status.OK)
                {
                    Debug.LogError("Reconfiguring daemon failed");
                    return result;
                }

                Debug.Log($"Restarting daemon with new port {newPort}...");
                bannedAPIPorts.Add(newPort);

                StartDaemon(false);

                await Task.Delay(5000);
            }

            // Too many retries
            return Status.CommandFailed;
        }
    }
}