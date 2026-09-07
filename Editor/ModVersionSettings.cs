using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace DaftAppleGames.Editor.ModPublisher
{
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
    [HideMonoScript]
#endif
    public sealed class ModVersionSettings : ScriptableObject
    {
        private const string SettingsAssetPath =
            "Assets/ModVersionSettings.asset";

        private static ModVersionSettings instance;

#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [Title("Configured Mods", "Version sources, build manifests, and publishing targets.")]
        [ListDrawerSettings(
            DefaultExpandedState = true,
            DraggableItems = true,
            ShowPaging = false)]
#endif
        [SerializeField] private List<ModVersionEntry> mods = new List<ModVersionEntry>();

        public static ModVersionSettings Instance => instance == null ? LoadOrCreate() : instance;
        public IReadOnlyList<ModVersionEntry> Mods => mods;

        /// <summary>
        /// Saves the project-specific mod version configuration
        /// </summary>
        public void SaveSettings()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private static ModVersionSettings LoadOrCreate()
        {
            instance = AssetDatabase.LoadAssetAtPath<ModVersionSettings>(SettingsAssetPath);
            if (instance != null)
            {
                return instance;
            }

            instance = CreateInstance<ModVersionSettings>();
            AssetDatabase.CreateAsset(instance, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            return instance;
        }
    }
}
