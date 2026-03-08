using Newtonsoft.Json;
using StorkStudios.CoreNest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
        [NotNull]
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

#if UNITY_EDITOR
            if (TelemetryDebugHandler != null)
            {
                TelemetryDebugHandler.OnWarning("Telemetry is enabled - it should be only enabled in production builds!");
            }
#else
            if (TelemetryDebugHandler != null)
            {
                TelemetryDebugHandler.OnInfo($"Telemetry running");
            }
#endif
            Init();
            foreach (ITelemetryPlugin plugin in plugins)
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
            SendSystemInfoMessage();
        }

        private void OnApplicationQuit()
        {
            if (!enableTelemetry || serverStatus == ServerStatus.Offline)
            {
                return;
            }

            SendTelemetryMessage(new TelemetryMessage(TelemetryMessageType.ApplicationQuit));
        }

        protected override void OnDestroy()
        {
            foreach (ITelemetryPlugin plugin in plugins)
            {
                plugin.OnBeforeTelemetryDestroyed();
            }
        }

        public void SendTelemetryMessage(TelemetryMessage message)
        {
            //In a short window after startup we are sending messages to the server with unknown status
            //If server is running it will get the message, if not we will handle the error in DataWaste
            //When server status is determined as offline, we stop
            if (!enableTelemetry || serverStatus == ServerStatus.Offline)
            {
                return;
            }

            foreach (ITelemetryPlugin plugin in plugins)
            {
                plugin.OnBeforeMessageSent(message);
            }

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "timestamp", DateTime.UtcNow },
                { "data", message.Data }
            };
            UnityWebRequest request = UnityWebRequest.Post(telemetryServerAddress + $"/telemetry/{gameId}",
                JsonConvert.SerializeObject(data),
                "application/json");
            StartCoroutine(HandleRequest(request));
        }

        /**
         * TODO: komentarz API
         */
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
                    if (TelemetryDebugHandler != null)
                    {
                        TelemetryDebugHandler.OnError(request.error);
                    }
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
                    if (debugInfo && TelemetryDebugHandler != null)
                    {
                        TelemetryDebugHandler.OnWarning("Telemetry server is unavailable");
                    }
                }
                else
                {
                    serverStatus = ServerStatus.Online;
                    if (debugInfo && TelemetryDebugHandler != null)
                    {
                        TelemetryDebugHandler.OnInfo("Telemetry server is running");
                    }
                }
            }));
        }

        private void SendApplicationStartMessage()
        {
            TelemetryMessage message = new TelemetryMessage(TelemetryMessageType.ApplicationStart);
            SendTelemetryMessage(message);
        }

        private void SendSystemInfoMessage()
        {
            TelemetryMessage message = new TelemetryMessage(TelemetryMessageType.SystemInfo);
            Type systemInfoType = typeof(SystemInfo);
            foreach (PropertyInfo info in systemInfoType.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                message.AddProperty(info.Name, info.GetValue(systemInfoType, null).ToString());
            }
            SendTelemetryMessage(message);
        }

#if UNITY_EDITOR
        [InvokeButton("Test server")]
        private void TestServerStatus()
        {
            UnityWebRequest request = UnityWebRequest.Get(telemetryServerAddress + "/status");
            StartCoroutine(HandleRequest(request, (result) =>
            {
                if (result == null)
                {
                    if (TelemetryDebugHandler != null)
                    {
                        TelemetryDebugHandler.OnWarning("Telemetry server is unavailable");
                    }
                }
                else
                {
                    if (TelemetryDebugHandler != null)
                    {
                        TelemetryDebugHandler.OnInfo("Telemetry server is running");
                    }
                }
            }));
        }
#endif
    }
}