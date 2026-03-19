using UnityEngine;

namespace StorkStudios.DataWaste
{
    [CreateAssetMenu(fileName = "RunIdTelemetryPlugin", menuName = "StorkStudios/DataWaste/Telemetry plugins/RunIdTelemetryPlugin")]
    public class RunIndexTelemetryPlugin : ScriptableObject, ITelemetryPlugin
    {
        private int runIndex;

        public void OnBeforeMessageSent(TelemetryMessage message)
        {
            message.AddProperty("runIndex", runIndex);
        }

        public void OnTelemetryInitialized()
        {
            runIndex = GetAndUpdateRunIndex();
        }

        public void OnBeforeTelemetryDestroyed() { }

        private int GetAndUpdateRunIndex()
        {
            int index = PlayerPrefs.GetInt("RunIndex", 0);
            index++;
            PlayerPrefs.SetInt("RunIndex", index);
            return index;
        }
    }
}