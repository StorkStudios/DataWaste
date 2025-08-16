using System;
using UnityEngine;

public interface ITelemetryErrorHandler
{
    public void HandleError(string errorText);
}
