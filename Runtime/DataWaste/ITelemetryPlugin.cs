using UnityEngine;

namespace StorkStudios.DataWaste
{
    public interface ITelemetryPlugin
    {
        public void OnTelemetryInitialized();

        public void OnBeforeMessageSent(TelemetryMessage message);

        public void OnBeforeTelemetryDestroyed();
    }
}