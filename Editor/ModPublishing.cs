using System;
using System.Threading;
using System.Threading.Tasks;

namespace DaftAppleGames.Editor.ModPublisher
{
    public sealed class ModPublishingContext
    {
        public ModPublishingContext(
            ModVersionEntry entry,
            string archivePath,
            string changelog,
            string nexusApiKey,
            string gitHubOwner,
            string gitHubToken)
        {
            Entry = entry;
            ArchivePath = archivePath;
            Changelog = changelog;
            NexusApiKey = nexusApiKey;
            GitHubOwner = gitHubOwner;
            GitHubToken = gitHubToken;
        }

        public ModVersionEntry Entry { get; }
        public string ArchivePath { get; }
        public string Changelog { get; }
        public string NexusApiKey { get; }
        public string GitHubOwner { get; }
        public string GitHubToken { get; }
        public string Version => Entry.Version.ToString();
    }

    public sealed class ModPublishingResult
    {
        public ModPublishingResult(string identifier, string message)
        {
            Identifier = identifier;
            Message = message;
        }

        public string Identifier { get; }
        public string Message { get; }
    }

    public interface IModPublishingTarget
    {
        ModHostingSite Site { get; }
        string DisplayName { get; }
        bool TryValidate(ModPublishingContext context, out string error);
        string Describe(ModPublishingContext context);
        Task<ModPublishingResult> PublishAsync(
            ModPublishingContext context,
            IProgress<UploadProgress> progress,
            CancellationToken cancellationToken);
    }
}
