using Newtonsoft.Json;
using StorkStudios.CoreNest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace StorkStudios.DataWaste
{
    public class Telemetry : Singleton<Telemetry>
    {
        private enum ServerStatus
        {
            Unknown,
            Online,
            Offline
        }

        [Header("Config")]
        [SerializeField]
        [Tooltip("This should be disabled during development, to limit load on the telemetry server")]
        private bool enableTelemetry;
        [SerializeField]
        private string telemetryServerAddress;
        [SerializeField]
        private string gameId;
        [SerializeField]
        private int timeout;
        [SerializeField]
        [RequireInterface(typeof(ITelemetryPlugin))]
        private List<ScriptableObject> plugins;

        [SerializeField]
        [RequireInterface(typeof(ITelemetryDebugHandler))]
        private ScriptableObject telemetryErrorHandler;

        [Header("Sent data")]
        [SerializeField]
        private bool sendInitPackage = true;

        [Header("Debug")]
        [SerializeField]
        private bool debugInfo;

        [SerializeField]
        [ReadOnly]
        private ServerStatus serverStatus = ServerStatus.Unknown;

        private ITelemetryDebugHandler TelemetryDebugHandler => telemetryErrorHandler as ITelemetryDebugHandler;

        protected override void Awake()
        {
            if (!enableTelemetry)
            {
                return;
            }
            if (TelemetryDebugHandler == null)
            {
                telemetryErrorHandler = ScriptableObject.CreateInstance(typeof(DefaultTelemetryErrorHandler));
            }

#if UNITY_EDITOR
            TelemetryDebugHandler.OnWarning("Telemetry is enabled - it should be only enabled in production builds!");
#else
            TelemetryDebugHandler.OnInfo($"Telemetry running");
#endif
            Init();
            foreach (ITelemetryPlugin plugin in plugins.Cast<ITelemetryPlugin>())
            {
                plugin.OnTelemetryInitialized();
            }

            base.Awake();
        }

        private void Start()
        {
            if (!enableTelemetry || !sendInitPackage)
            {
                return;
            }

            SendApplicationStartMessage();
        }

        private void OnApplicationQuit()
        {
            if (!enableTelemetry || serverStatus == ServerStatus.Offline)
            {
                return;
            }

            SendTelemetryMessage(new(TelemetryMessageType.ApplicationQuit));
        }

        protected override void OnDestroy()
        {
            foreach (ITelemetryPlugin plugin in plugins.Cast<ITelemetryPlugin>())
            {
                plugin.OnBeforeTelemetryDestroyed();
            }

            base.OnDestroy();
        }

        /// <summary>
        /// Send a telemetry message to the server.
        /// The method does nothing if the server is offline or telemetry is disabled so no checks are required before calling it.
        /// </summary>
        public void SendTelemetryMessage(TelemetryMessage message)
        {
            /*
             * In a short window after startup we are sending messages to the server with unknown status.
             * If server is running it will get the message, if it's not we will handle the error elswhere.
             * When server status is determined as offline, we stop.
            */
            if (!enableTelemetry || serverStatus == ServerStatus.Offline)
            {
                return;
            }

            foreach (ITelemetryPlugin plugin in plugins.Cast<ITelemetryPlugin>())
            {
                plugin.OnBeforeMessageSent(message);
            }

            Dictionary<string, object> data = new()
            {
                { "timestamp", DateTime.UtcNow },
                { "data", message.Data }
            };
            UnityWebRequest request = UnityWebRequest.Post(telemetryServerAddress + $"/telemetry/{gameId}",
                JsonConvert.SerializeObject(data),
                "application/json");
            StartCoroutine(HandleRequest(request));
        }

        /// <summary>
        /// Fetch data from the telemetry server. The endpoint path is "/extras/gameId".
        /// </summary> 
        /// <param name="callback"> Path to the specific resources. It will be added to the end of the request URL.</param>
        /// <param name="errorCallback"> Callback function called after receiving the data.</param>
        /// <param name="path"> Callback function called after receiving an error response.</param>
        public void GetData<T>(string path, Action<T> callback, Action<string> errorCallback = null) where T : class
        {
            if (!enableTelemetry || serverStatus == ServerStatus.Offline)
            {
                return;
            }

            UnityWebRequest request = UnityWebRequest.Get(telemetryServerAddress + $"/extras/{gameId}/{path}");
            StartCoroutine(HandleRequest(request,
                (result) =>
                {
                    T deserializedResult;
                    if (typeof(T) == typeof(string))
                    {
                        deserializedResult = result as T;
                    }
                    else
                    {
                        deserializedResult = JsonConvert.DeserializeObject<T>(result);
                    }
                    callback(deserializedResult);
                },
                errorCallback));
        }

        private IEnumerator HandleRequest(UnityWebRequest request, Action<string> callback = null, Action<string> errorCallback = null)
        {
            request.timeout = timeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (callback != null)
                {
                    callback(request.downloadHandler.text);
                }
            }
            else
            {
                if (errorCallback != null)
                {
                    errorCallback(request.error);
                }
                else
                {
                    TelemetryDebugHandler.OnError(request.error);
                }
            }
        }

        private void Init()
        {
            UnityWebRequest request = UnityWebRequest.Get(telemetryServerAddress + "/status");
            StartCoroutine(HandleRequest(request, (result) =>
            {
                if (result == null)
                {
                    serverStatus = ServerStatus.Offline;
                    if (debugInfo)
                    {
                        TelemetryDebugHandler.OnWarning("Telemetry server is unavailable");
                    }
                }
                else
                {
                    serverStatus = ServerStatus.Online;
                    if (debugInfo)
                    {
                        TelemetryDebugHandler.OnInfo("Telemetry server is running");
                    }
                }
            }));
        }

        private void SendApplicationStartMessage()
        {
            TelemetryMessage message = new(TelemetryMessageType.ApplicationStart);
            SendTelemetryMessage(message);
        }

#if UNITY_EDITOR
        [InvokeButton("Test server")]
        private void TestServerStatus()
        {
            UnityWebRequest request = UnityWebRequest.Get(telemetryServerAddress + "/status");
            if (TelemetryDebugHandler == null)
            {
                telemetryErrorHandler = ScriptableObject.CreateInstance(typeof(DefaultTelemetryErrorHandler));
            }
            StartCoroutine(HandleRequest(request, (result) =>
            {
                if (result == null)
                {
                    TelemetryDebugHandler.OnWarning("Telemetry server is unavailable");
                }
                else
                {
                    TelemetryDebugHandler.OnInfo("Telemetry server is running");
                }
            }));
        }
#endif
        private class DefaultTelemetryErrorHandler : ScriptableObject, ITelemetryDebugHandler
        {
            public void OnError(string message)
            {
                Debug.LogError($"Telemetry error: {message}");
            }

            public void OnInfo(string message)
            {
                Debug.Log($"Telemetry info: {message}");
            }

            public void OnWarning(string message)
            {
                Debug.LogWarning($"Telemetry warining: {message}");
            }
        }
    }
}