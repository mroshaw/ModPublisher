using System;
using UnityEngine;

namespace DaftAppleGames.Editor.ModPublisher
{
    [Serializable]
    public sealed class GitHubReleaseOptions
    {
        [Tooltip("Repository name without the owner or .git suffix.")]
        [SerializeField] private string repositoryName;
        [Tooltip("Tokens: {displayName}, {archiveName}, {version}.")]
        [SerializeField] private string tagNameTemplate = "{archiveName}-v{version}";
        [Tooltip("Tokens: {displayName}, {archiveName}, {version}.")]
        [SerializeField] private string releaseTitleTemplate = "{displayName} v{version}";
        [Tooltip("Optional branch or commit. Leave empty to use the repository's default branch.")]
        [SerializeField] private string targetCommitish;
        [SerializeField] private bool draft;
        [SerializeField] private bool prerelease;
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
