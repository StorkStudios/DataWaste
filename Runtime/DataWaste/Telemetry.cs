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
    public class Telemetry : PersistentSingleton<Telemetry>
    {
        private const string serverStatusOkString = "OK";
        
        private enum ServerStatus
        {
            Unknown,
            Online,
            Offline
        }

        [SerializeField]
        [ReadOnly]
        private ServerStatus serverStatus = ServerStatus.Unknown;

        protected override void Awake()
        {
            if (!TelemetryConfiguration.Instance.EnableTelemetry)
            {
                return;
            }

#if UNITY_EDITOR
            TelemetryConfiguration.Instance.TelemetryErrorHandler.OnWarning("Telemetry is enabled - it should be only enabled in production builds!");
#else
            TelemetryConfiguration.Instance.TelemetryErrorHandler.OnInfo($"Telemetry running");
#endif
            Init();
            foreach (ITelemetryPlugin plugin in TelemetryConfiguration.Instance.Plugins.Cast<ITelemetryPlugin>())
            {
                plugin.OnTelemetryInitialized();
            }

            base.Awake();
        }

        private void Start()
        {
            if (!TelemetryConfiguration.Instance.EnableTelemetry || !TelemetryConfiguration.Instance.SendInitPackage)
            {
                return;
            }

            SendApplicationStartMessage();
        }

        private void OnApplicationQuit()
        {
            if (!TelemetryConfiguration.Instance.EnableTelemetry || serverStatus == ServerStatus.Offline)
            {
                return;
            }

            SendTelemetryMessage(new(TelemetryMessageType.ApplicationQuit));
        }

        protected override void OnDestroy()
        {
            foreach (ITelemetryPlugin plugin in TelemetryConfiguration.Instance.Plugins.Cast<ITelemetryPlugin>())
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
            if (!TelemetryConfiguration.Instance.EnableTelemetry || serverStatus == ServerStatus.Offline)
            {
                return;
            }

            foreach (ITelemetryPlugin plugin in TelemetryConfiguration.Instance.Plugins.Cast<ITelemetryPlugin>())
            {
                plugin.OnBeforeMessageSent(message);
            }

            Dictionary<string, object> data = new()
            {
                { "timestamp", DateTime.UtcNow },
                { "data", message.Data }
            };
            try
            {
                UnityWebRequest request = UnityWebRequest.Post(
                    TelemetryConfiguration.Instance.TelemetryServerAddress + $"/telemetry/{TelemetryConfiguration.Instance.GameId}",
                    JsonConvert.SerializeObject(data),
                    "application/json");
                StartCoroutine(HandleRequest(request));
            }
            catch (Exception ex)
            {
                TelemetryConfiguration.Instance.TelemetryErrorHandler.OnError(ex.ToString());
            }
        }

        /// <summary>
        /// Fetch data from the telemetry server. The endpoint path is "/extras/gameId".
        /// </summary> 
        /// <param name="path"> Path to the specific resources. It will be added to the end of the request URL.</param>
        /// <param name="callback"> Callback function called after receiving the data.</param>
        /// <param name="errorCallback"> Callback function called after receiving an error response.</param>
        public void GetData<T>(string path, Action<T> callback, Action<string> errorCallback = null) where T : class
        {
            if (!TelemetryConfiguration.Instance.EnableTelemetry || serverStatus == ServerStatus.Offline)
            {
                return;
            }

            UnityWebRequest request = UnityWebRequest.Get(TelemetryConfiguration.Instance.TelemetryServerAddress + $"/extras/{TelemetryConfiguration.Instance.GameId}/{path}");
            StartCoroutine(HandleRequest(request,
                (result) =>
                {
                    T deserializedResult;
                    if (typeof(T) == typeof(string))
                    {
                        deserializedResult = result as T;
                        callback?.Invoke(deserializedResult);
                    }
                    else
                    {
                        try
                        {
                            deserializedResult = JsonConvert.DeserializeObject<T>(result);
                            callback?.Invoke(deserializedResult);
                        }
                        catch (Exception ex)
                        {
                            if (errorCallback != null)
                            {
                                errorCallback(ex.ToString());
                            }
                            else
                            {
                                TelemetryConfiguration.Instance.TelemetryErrorHandler.OnError(ex.ToString());
                            }
                        }
                    }
                },
                errorCallback));
        }

        private IEnumerator HandleRequest(UnityWebRequest request, Action<string> callback = null, Action<string> errorCallback = null)
        {
            request.timeout = TelemetryConfiguration.Instance.Timeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(request.downloadHandler.text);
            }
            else
            {
                if (errorCallback != null)
                {
                    errorCallback(request.error);
                }
                else
                {
                    TelemetryConfiguration.Instance.TelemetryErrorHandler.OnError(request.error);
                }
            }
        }

        private void Init()
        {
            UnityWebRequest request = UnityWebRequest.Get(TelemetryConfiguration.Instance.TelemetryServerAddress + "/status");
            StartCoroutine(HandleRequest(request, (result) =>
            {
                if (result == null || result != serverStatusOkString)
                {
                    serverStatus = ServerStatus.Offline;
                    if (TelemetryConfiguration.Instance.PrintDebugInfo)
                    {
                        TelemetryConfiguration.Instance.TelemetryErrorHandler.OnWarning("Telemetry server is unavailable");
                    }
                }
                else
                {
                    serverStatus = ServerStatus.Online;
                    if (TelemetryConfiguration.Instance.PrintDebugInfo)
                    {
                        TelemetryConfiguration.Instance.TelemetryErrorHandler.OnInfo("Telemetry server is running");
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
            UnityWebRequest request = UnityWebRequest.Get(TelemetryConfiguration.Instance.TelemetryServerAddress + "/status");
            StartCoroutine(HandleRequest(request, (result) =>
            {
                if (result == null)
                {
                    TelemetryConfiguration.Instance.TelemetryErrorHandler.OnWarning("Telemetry server is unavailable");
                }
                else if (result == serverStatusOkString)
                {
                    TelemetryConfiguration.Instance.TelemetryErrorHandler.OnInfo("Telemetry server is running");
                }
                else
                {
                    TelemetryConfiguration.Instance.TelemetryErrorHandler.OnWarning($"Unexpected response from the telemetry server: {result}");
                }
            }));
        }
#endif
    }
}