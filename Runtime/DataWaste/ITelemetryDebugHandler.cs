using System;
using UnityEngine;

public interface ITelemetryDebugHandler
{
    public void OnError(string message);
    public void OnWarning(string message);
    public void OnInfo(string message);
}
