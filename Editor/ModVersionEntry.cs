using System;
using ThunderKit.Core.Manifests;
using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace DaftAppleGames.Editor.ModPublisher
{
    [Serializable]
    public class ModVersionEntry
    {
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [Required("Enter the friendly mod name used in release titles.")]
#endif
        [SerializeField] private string name;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [AssetsOnly]
        [Required("Assign the BepInEx plugin script used for version updates.")]
#endif
        [SerializeField] private MonoScript pluginScript;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [BoxGroup("Identity")]
        [AssetsOnly]
        [Required("Assign the ThunderKit manifest used to locate the staged ZIP.")]
#endif
        [SerializeField] private Manifest manifest;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [BoxGroup("Versions")]
        [InlineProperty]
        [LabelText("Next Version")]
#endif
        [SerializeField] private ModVersion version = new ModVersion();
        [SerializeField, HideInInspector] private ModVersion currentPublishedVersion = new ModVersion();
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [BoxGroup("Publishing")]
        [EnumToggleButtons]
        [LabelText("Publishing Sites")]
#endif
        [Tooltip("Select every hosting site that receives this mod when Publish is clicked.")]
        [SerializeField] private ModHostingSite publishingSites = ModHostingSite.Nexus;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [ShowIf(nameof(IsNexusEnabled))]
        [FoldoutGroup("Nexus Mods", false)]
        [LabelText("Nexus Options")]
#endif
        [SerializeField] private NexusModsUploadOptions nexusMods = new NexusModsUploadOptions();
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [ShowIf(nameof(IsGitHubEnabled))]
        [FoldoutGroup("GitHub Release", false)]
        [LabelText("GitHub Options")]
#endif
        [SerializeField] private GitHubReleaseOptions gitHub = new GitHubReleaseOptions();

        public string Name => name;
        public MonoScript PluginScript => pluginScript;
        public Manifest Manifest => manifest;
        public ModVersion Version => version;
        public ModVersion CurrentPublishedVersion => currentPublishedVersion;
        public ModHostingSite PublishingSites => publishingSites;
        public NexusModsUploadOptions NexusMods => nexusMods;
        public GitHubReleaseOptions GitHub => gitHub;
        public bool PublishesTo(ModHostingSite site) => (publishingSites & site) == site;

#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        private bool IsNexusEnabled => PublishesTo(ModHostingSite.Nexus);
        private bool IsGitHubEnabled => PublishesTo(ModHostingSite.GitHub);
#endif
    }
}
