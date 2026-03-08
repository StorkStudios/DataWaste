using UnityEngine;

namespace StorkStudios.DataWaste
{
    [CreateAssetMenu(fileName = "RunIdTelemetryPlugin", menuName = "StorkStudios/DataWaste/Telemetry plugins/RunIdTelemetryPlugin")]
    public class RunIdTelemetryPlugin : ScriptableObject, ITelemetryPlugin
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

        private int GetAndUpdateRunIndex()
        {
            int idx = PlayerPrefs.GetInt("RunIndex", 0);
            idx++;
            PlayerPrefs.SetInt("RunIndex", idx);
            return idx;
        }
    }
}