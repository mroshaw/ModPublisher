using System;
using UnityEngine;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace DaftAppleGames.Editor.ModPublisher
{
    [Serializable]
    public sealed class GitHubReleaseOptions
    {
        [Tooltip("Repository name without the owner or .git suffix.")]
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [Required("Enter the repository name without its owner or .git suffix.")]
        [InfoBox("The repository owner and access token are configured once in Publishing Connections.", InfoMessageType.None)]
#endif
        [SerializeField] private string repositoryName;
        [Tooltip("Tokens: {displayName}, {archiveName}, {version}.")]
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [PropertyTooltip("Tokens: {displayName}, {archiveName}, and {version}.")]
#endif
        [SerializeField] private string tagNameTemplate = "{archiveName}-v{version}";
        [Tooltip("Tokens: {displayName}, {archiveName}, {version}.")]
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [PropertyTooltip("Tokens: {displayName}, {archiveName}, and {version}.")]
#endif
        [SerializeField] private string releaseTitleTemplate = "{displayName} v{version}";
        [Tooltip("Optional branch or commit. Leave empty to use the repository's default branch.")]
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [SuffixLabel("optional")]
#endif
        [SerializeField] private string targetCommitish;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [ToggleLeft]
#endif
        [SerializeField] private bool draft;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [ToggleLeft]
#endif
        [SerializeField] private bool prerelease;
#if ODIN_INSPECTOR && !DEBUG_NO_ODIN_INSPECTOR
        [ToggleLeft]
#endif
        [SerializeField] private bool generateReleaseNotes;

        public string RepositoryName => repositoryName;
        public string TagNameTemplate => tagNameTemplate;
        public string ReleaseTitleTemplate => releaseTitleTemplate;
        public string TargetCommitish => targetCommitish;
        public bool Draft => draft;
        public bool Prerelease => prerelease;
        public bool GenerateReleaseNotes => generateReleaseNotes;
    }
}
