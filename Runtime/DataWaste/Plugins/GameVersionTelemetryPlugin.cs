using StorkStudios.CoreNest;
using UnityEngine;

namespace StorkStudios.DataWaste
{
    [CreateAssetMenu(fileName = "GameVersionTelemetryPlugin", menuName = "StorkStudios/DataWaste/Telemetry plugins/GameVersionTelemetryPlugin")]
    public class GameVersionTelemetryPlugin : ScriptableObject, ITelemetryPlugin
    {
        public const string GameVersionPropertyName = "version";

        public void OnTelemetryInitialized()
        {
            CheckNewGameVersion();
        }
        
        public void OnBeforeMessageSent(TelemetryMessage message)
        {
            if (message.MessageType == TelemetryMessageType.ApplicationStart)
            {
                message.AddProperty(GameVersionPropertyName, GameVersion.Instance.VersionText);
            }
        }

        private void CheckNewGameVersion()
        {
            if (GameVersion.Instance == null)
            {
                Debug.LogError("GameVersion singleton not found, can't check game version. Disable GameVersionTelemetryPlugin or create GameVersion singleton in Resources folder.");
                return;
            }

            Telemetry.Instance.GetData<GameVersionData>("version", OnGameVersionReceived, (error) => Debug.LogError($"Error while fetching game version: {error}"));
        }

        private void OnGameVersionReceived(GameVersionData version)
        {
            if (version == null)
            {
                return;
            }

            if (version.VersionIndex > GameVersion.Instance.VersionIndex)
            {
                Debug.Log($"New game version is available - {version.VersionName}");
                GameVersion.Instance.NewestAvailableVersion.Value = version;
            }
        }
    }
}
