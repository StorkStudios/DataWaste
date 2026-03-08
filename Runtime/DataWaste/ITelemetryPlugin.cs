using UnityEngine;

namespace StorkStudios.DataWaste
{
    public interface ITelemetryPlugin
    {
        public virtual void OnBeforeMessageSent(TelemetryMessage message)
        {

        }

        public virtual void OnTelemetryInitialized()
        {

        }

        public virtual void OnBeforeTelemetryDestroyed()
        {

        }
    }
}