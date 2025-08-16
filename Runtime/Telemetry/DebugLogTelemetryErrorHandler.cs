using UnityEngine;
using System;
using StorkStudios.CoreNest;

[CreateAssetMenu(fileName = "DebugLogTelemetryErrorHandler", menuName = "StorkStudios/DataWaste/DebugLogTelemetryErrorHandler")]
public class DebugLogTelemetryErrorHandler : ScriptableObjectSingleton<DebugLogTelemetryErrorHandler>, ITelemetryErrorHandler
{
    public void HandleError(Exception exception)
    {
        Debug.LogError($"Telemetry Error: {exception.Message}\n{exception.StackTrace}");
    }
}
