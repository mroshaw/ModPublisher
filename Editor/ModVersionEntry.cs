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
        [SerializeField] private NexusModsUploadOptions nexusMods = new NexusModsUploadOptions();

        public string Name => name;
        public MonoScript PluginScript => pluginScript;
        public Manifest Manifest => manifest;
        public ModVersion Version => version;
        public ModVersion CurrentPublishedVersion => currentPublishedVersion;
        public NexusModsUploadOptions NexusMods => nexusMods;
    }
}
