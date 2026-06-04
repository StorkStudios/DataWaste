using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StorkStudios.CoreNest;

namespace StorkStudios.DataWaste
{
    [CreateAssetMenu(fileName = "GameVersion", menuName = "StorkStudios/DataWaste/Game version")]
    public class GameVersion : ScriptableObjectSingleton<GameVersion>
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