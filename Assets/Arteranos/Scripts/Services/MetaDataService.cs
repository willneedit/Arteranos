/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Mirror;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Arteranos.Services
{
    public class MetaDataService : MonoBehaviour
    {
        private bool serverActive = false;

        private IEnumerator MDServiceCoroutine = null;

        void Start()
        {
            MDServiceCoroutine = ManageMetaDataServer();
            StartCoroutine(MDServiceCoroutine);
        }

        private void OnDestroy()
        {
            if(MDServiceCoroutine != null) StopCoroutine(MDServiceCoroutine);
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

    internal class MetadataJSON
    {
        public ServerSettingsJSON Settings = null;
        public string CurrentWorld = null;
        public List<string> CurrentUsers = new();
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

                //Debug.Log($"[{nameof(MetaDataServer)}] [{request.HttpMethod}] {request.Url.AbsolutePath}");

                //if(request.HttpMethod == "GET" && request.Url.AbsolutePath == "/favicon.ico")
                //{
                //    // Ignore.
                //}
                //else 
                if(request.HttpMethod == "GET" && request.Url.AbsolutePath == "/metadata.json")
                {
                    YieldMetadata(response);
                }
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

        private static async void YieldMetadata(HttpListenerResponse response)
        {
            MetadataJSON mdj = new()
            {
                Settings = SettingsManager.Server,
                CurrentWorld = SettingsManager.Server.WorldURL,
                CurrentUsers = SettingsManager.Users
            };

            string json = JsonConvert.SerializeObject(mdj);

            byte[] data = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = data.LongLength;
            response.StatusCode = (int) HttpStatusCode.OK;

            await response.OutputStream.WriteAsync(data, 0, data.Length);
            response.Close();

        }
    }
}
