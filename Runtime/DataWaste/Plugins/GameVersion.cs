using UnityEngine;
using StorkStudios.CoreNest;

namespace StorkStudios.DataWaste
{
    [Singleton]
    [CreateAssetMenu(fileName = "GameVersion", menuName = "StorkStudios/DataWaste/Game version")]
    public sealed partial class GameVersion : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Version name that is diplayed in game")]
        private string versionText;
        public string VersionText => versionText;

        [SerializeField]
        [Tooltip("Version number that should be incremented with every release. It is used to compare versions")]
        private int versionIndex;
        public int VersionIndex => versionIndex;

        private ObservableVariable<GameVersionData> newestAvailableVersion = new();
        public ObservableVariable<GameVersionData> NewestAvailableVersion => newestAvailableVersion;
    }
}