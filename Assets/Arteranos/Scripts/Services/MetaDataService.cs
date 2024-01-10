/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Arteranos.Services
{
    public class MetaDataService : MonoBehaviour
    {
        private const string PATH_USER_PRIVACY_NOTICE = "Privacy_TOS_Notice.md";
        private bool serverActive = false;

        private IEnumerator MDServiceCoroutine = null;

        public static string CachedPTOSNotice { get; private set; } = null;

        void Start()
        {
            // If it doesn't exist, write down the template in the config directory.
            if(!FileUtils.ReadConfig(PATH_USER_PRIVACY_NOTICE, File.Exists))
            {
                FileUtils.WriteTextConfig(PATH_USER_PRIVACY_NOTICE, Core.Utils.LoadDefaultTOS());
                Debug.LogWarning("Privacy notice and Terms Of Service template written down - Read (and modify) according to your use case!");
            }

            CachedPTOSNotice = FileUtils.ReadTextConfig(PATH_USER_PRIVACY_NOTICE);

            MDServiceCoroutine = ManageMetaDataServer();
            StartCoroutine(MDServiceCoroutine);
        }

        private void OnDestroy()
        {
            if(MDServiceCoroutine != null) StopCoroutine(MDServiceCoroutine);
            MetaDataServer.Stop();
        }

        private IEnumerator ManageMetaDataServer()
        {
            while(true)
            {
                yield return new WaitForSeconds(2);

                if(NetworkServer.active && !serverActive)
                {
                    MetaDataServer.Start();
                    serverActive = true;
                }
                else if(!NetworkServer.active && serverActive)
                {
                    MetaDataServer.Stop();
                    serverActive = false;
                }
            }
        }
    }

    internal class MetaDataServer
    {
        private static HttpListener _httpListener = null;
        private static Task _listenTask = null;
        private static bool _runServer = false;

        public static void Start()
        {
            if(_runServer) return;

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://*:{SettingsManager.Server.MetadataPort}/");
            _httpListener.Start();
            _runServer = true;

            _listenTask = HandleIncomingConnectionsAsync();
            Debug.Log($"[{nameof(MetaDataServer)}] Listening for connections on port '{SettingsManager.Server.MetadataPort}'");
        }

        public static void Stop()
        {
            if(!_runServer) return;

            _runServer = false;

            _httpListener?.Stop();
            _httpListener?.Close();
            _httpListener = null;
            Debug.Log($"[{nameof(MetaDataServer)}] Stopped listening for Metadata server.");
        }

        private static async Task HandleIncomingConnectionsAsync()
        {
            Debug.Log($"[{nameof(MetaDataServer)}] Entering md server loop");

            while(_runServer)
            {
                HttpListenerContext ctx = await _httpListener.GetContextAsync();
                HttpListenerRequest request = ctx.Request;
                HttpListenerResponse response = ctx.Response;

                // Debug.Log($"[{nameof(MetaDataServer)}] [{request.HttpMethod}] {request.Url.AbsolutePath}");

                if(request.HttpMethod != "GET")
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Close();
                    continue;
                }

                else if (request.Url.AbsolutePath == ServerOnlineData.urlPathPart)
                    YieldServerOnlineData(response);
                else if (request.Url.AbsolutePath == "/")
                    YieldLaunchPage(request.Url.Host, response);
                else
                {
                    // Debug.LogWarning($"[{nameof(MetaDataServer)}] Invalid request.");

                    response.StatusCode = (int) HttpStatusCode.NotFound;
                    response.Close();
                }
            }

            // NOTREACHED
            Debug.Log($"[{nameof(MetaDataServer)}] Exiting md server loop");
        }

        private static async void YieldServerOnlineData(HttpListenerResponse response)
        {
            ServerOnlineData serverOnlineData = new()
            {
                CurrentWorld = SettingsManager.CurrentWorld,
                CurrentWorldName = SettingsManager.CurrentWorldName,
                UserFingerprints = (from user in NetworkStatus.GetOnlineUsers()
                                   where user.UserPrivacy != null && user.UserPrivacy.Visibility != Core.Visibility.Invisible
                                   select CryptoHelpers.GetFingerprint(user.UserID)).ToList(),
            };

            await Core.Utils.WebEmit(serverOnlineData, response);
        }

        private  static async void YieldLaunchPage(string hostname, HttpListenerResponse response)
        {

            string linkto = $"arteranos://{hostname}:{SettingsManager.Server.MetadataPort}/";
            string html
= "<html>\n"
+ "<head>\n"
+ "<title>Launch Arteranos connection</title>\n"
+$"<meta http-equiv=\"refresh\" content=\"0; url={linkto}\" />\n"
+ "</head>\n"
+ "<body>\n"
+$"Trouble with redirection? <a href=\"{linkto}\">Click here.</a>\n"
+ "</body>\n"
+ "</html>\n";

            byte[] data = Encoding.UTF8.GetBytes(html);
            response.ContentType = "text/html";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = data.LongLength;
            response.StatusCode = (int)HttpStatusCode.OK;

            await response.OutputStream.WriteAsync(data, 0, data.Length);
            response.Close();
        }
    }
}
