using System;
using UnityEngine;

namespace StorkStudios.DataWaste
{
    [CreateAssetMenu(fileName = "PlayerIdTelemetryPlugin", menuName = "StorkStudios/DataWaste/Telemetry plugins/PlayerIdTelemetryPlugin")]
    public class PlayerIdTelemetryPlugin : ScriptableObject, ITelemetryPlugin
    {
        private string playerId;

        public void OnBeforeMessageSent(TelemetryMessage message)
        {
            message.AddProperty("playerId", playerId);
        }

        public void OnTelemetryInitialized()
        {
#if UNITY_EDITOR
            playerId = "editor";
#else
            if (PlayerPrefs.HasKey("playerId"))
            {
                playerId = PlayerPrefs.GetString("playerId");
            }
            else
            {
                playerId = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("playerId", playerId);
            }
#endif
        }
    }
}
