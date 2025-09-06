using UnityEngine;
using System;
using StorkStudios.CoreNest;

namespace StorkStudios.DataWaste
{
    [CreateAssetMenu(fileName = "DebugLogTelemetryErrorHandler", menuName = "StorkStudios/DataWaste/DebugLogTelemetryErrorHandler")]
    public class DebugLogTelemetryErrorHandler : ScriptableObjectSingleton<DebugLogTelemetryErrorHandler>, ITelemetryDebugHandler
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