using System;
using UnityEngine;
using UnityEngine.Serialization;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace DaftAppleGames.Editor.ModPublisher
{
    [Serializable]
    public sealed class NexusModsUploadOptions
    {
        [FormerlySerializedAs("fileId")]
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [Required("Enter the Nexus file Group ID from the file's API Info dialog.")]
        [LabelText("File Group ID")]
#endif
        [SerializeField] private string fileGroupId;
        [FormerlySerializedAs("modId")]
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [LabelText("Game-scoped Mod ID")]
        [PropertyTooltip("Required when publishing a changelog. Taken from the Nexus mod page URL.")]
#endif
        [SerializeField] private string gameScopedModId;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [Required]
        [LabelText("Game Domain")]
#endif
        [SerializeField] private string gameDomain = "subnauticabelowzero";
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [PropertyTooltip("Optional asset display name. The ZIP filename is used when empty.")]
#endif
        [SerializeField] private string displayName;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [MultiLineProperty(4)]
#endif
        [TextArea(3, 8)]
        [SerializeField] private string description;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [Required]
#endif
        [SerializeField] private string fileCategory = "main";
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [ToggleLeft]
#endif
        [SerializeField] private bool archiveExistingVersion = true;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [ToggleLeft]
#endif
        [SerializeField] private bool updateModVersion = true;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [ToggleLeft]
#endif
        [SerializeField] private bool primaryModManagerDownload;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [ToggleLeft]
#endif
        [SerializeField] private bool allowModManagerDownload = true;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [ToggleLeft]
#endif
        [SerializeField] private bool showRequirementsPopup;

        public string FileGroupId => fileGroupId;
        public string GameScopedModId => gameScopedModId;
        public string GameDomain => gameDomain;
        public string DisplayName => displayName;
        public string Description => description;
        public string FileCategory => fileCategory;
        public bool ArchiveExistingVersion => archiveExistingVersion;
        public bool UpdateModVersion => updateModVersion;
        public bool PrimaryModManagerDownload => primaryModManagerDownload;
        public bool AllowModManagerDownload => allowModManagerDownload;
        public bool ShowRequirementsPopup => showRequirementsPopup;
    }
}
