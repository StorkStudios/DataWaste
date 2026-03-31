using StorkStudios.CoreNest;
using System.Collections.Generic;
using UnityEngine;

namespace StorkStudios.DataWaste
{
    [CreateAssetMenu(fileName = "TelemetryConfiguration", menuName = "StorkStudios/DataWaste/Telemetry configuration")]
    public class TelemetryConfiguration : ScriptableObjectSingleton<TelemetryConfiguration>
    {
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
        private bool printDebugInfo;

        public bool EnableTelemetry => enableTelemetry;
        public string TelemetryServerAddress => telemetryServerAddress;
        public string GameId => gameId;
        public int Timeout => timeout;
        public List<ScriptableObject> Plugins => plugins;
        public ITelemetryDebugHandler TelemetryErrorHandler
        {
            get
            {
                if (telemetryErrorHandler == null)
                {
                    telemetryErrorHandler = CreateInstance(typeof(DefaultTelemetryErrorHandler));
                }
                return telemetryErrorHandler as ITelemetryDebugHandler;
            }
        }

        public bool SendInitPackage => sendInitPackage;
        public bool PrintDebugInfo => printDebugInfo;

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