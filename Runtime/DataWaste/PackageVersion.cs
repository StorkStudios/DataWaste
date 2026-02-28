using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;

namespace StorkStudios.DataWaste
{
    public class PackageVersion : ScriptableObject
    {
        [HideInInspector]
        public string Version;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(object _)
        {
            var assembly = typeof(PackageVersion).Assembly;

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);

            var version = packageInfo.version;

            var guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(PackageVersion)}", new string[]{packageInfo.assetPath});
            
            PackageVersion asset;
            if(guids.Length > 0 && !string.IsNullOrWhiteSpace(guids[0]))
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                asset = AssetDatabase.LoadAssetAtPath<PackageVersion>(path);
            }
            else
            {

                asset = ScriptableObject.CreateInstance<PackageVersion>();
                asset.name = nameof(PackageVersion);
                asset.hideFlags = HideFlags.NotEditable;

                AssetDatabase.CreateAsset(asset, $"{packageInfo.assetPath}/{nameof(PackageVersion)}");
            }

            asset.Version = version;

            EditorUtility.SetDirty(asset);
        }
    }
}