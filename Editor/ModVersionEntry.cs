using System;
using ThunderKit.Core.Manifests;
using UnityEditor;
using UnityEngine;

namespace DaftAppleGames.Editor.ModPublisher
{
    [Serializable]
    public class ModVersionEntry
    {
        [SerializeField] private string name;
        [SerializeField] private MonoScript pluginScript;
        [SerializeField] private Manifest manifest;
        [SerializeField] private ModVersion version = new ModVersion();
        [SerializeField, HideInInspector] private ModVersion currentPublishedVersion = new ModVersion();
        [Tooltip("Select every hosting site that receives this mod when Publish is clicked.")]
        [SerializeField] private ModHostingSite publishingSites = ModHostingSite.Nexus;
        [SerializeField] private NexusModsUploadOptions nexusMods = new NexusModsUploadOptions();
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
    }
}
