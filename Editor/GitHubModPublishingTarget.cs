using System;
using System.Threading;
using System.Threading.Tasks;

namespace DaftAppleGames.Editor.ModPublisher
{
    public sealed class GitHubModPublishingTarget : IModPublishingTarget
    {
        public ModHostingSite Site => ModHostingSite.GitHub;
        public string DisplayName => "GitHub Releases";

        public bool TryValidate(ModPublishingContext context, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(context.GitHubToken))
            {
                error = "Enter a GitHub personal access token.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(context.GitHubOwner))
            {
                error = "Enter the default GitHub repository owner.";
                return false;
            }

            GitHubReleaseOptions options = context.Entry.GitHub;
            if (options == null || string.IsNullOrWhiteSpace(options.RepositoryName))
            {
                error = "Enter the GitHub repository name for this mod.";
                return false;
            }

            if (options.RepositoryName.Contains("/"))
            {
                error = "Enter only the GitHub repository name; the owner is configured in GitHub Connection.";
                return false;
            }

            string tagName = GetTagName(context);
            if (string.IsNullOrWhiteSpace(tagName))
            {
                error = "The GitHub tag template produces an empty tag.";
                return false;
            }

            string title = GetReleaseTitle(context);
            if (string.IsNullOrWhiteSpace(title))
            {
                error = "The GitHub release title template produces an empty title.";
                return false;
            }

            return true;
        }

        public string Describe(ModPublishingContext context) =>
            $"GitHub {context.GitHubOwner}/{context.Entry.GitHub.RepositoryName}, tag {GetTagName(context)}";

        public async Task<ModPublishingResult> PublishAsync(
            ModPublishingContext context,
            IProgress<UploadProgress> progress,
            CancellationToken cancellationToken)
        {
            GitHubReleaseOptions options = context.Entry.GitHub;
            using (GitHubReleasesApiClient client = new GitHubReleasesApiClient(context.GitHubToken))
            {
                GitHubReleaseResult result = await client.CreateReleaseWithAssetAsync(
                    context.GitHubOwner,
                    options.RepositoryName,
                    GetTagName(context),
                    GetReleaseTitle(context),
                    context.Changelog,
                    options,
                    context.ArchivePath,
                    progress,
                    cancellationToken);
                return new ModPublishingResult(
                    result.ReleaseId,
                    $"GitHub release: {result.HtmlUrl}");
            }
        }

        private static string GetTagName(ModPublishingContext context)
        {
            string template = string.IsNullOrWhiteSpace(context.Entry.GitHub.TagNameTemplate)
                ? ReleaseTemplate.DefaultTagName
                : context.Entry.GitHub.TagNameTemplate;
            return ReleaseTemplate.Expand(template, context.Entry, context.Version).Trim();
        }

        private static string GetReleaseTitle(ModPublishingContext context)
        {
            string template = string.IsNullOrWhiteSpace(context.Entry.GitHub.ReleaseTitleTemplate)
                ? ReleaseTemplate.DefaultReleaseTitle
                : context.Entry.GitHub.ReleaseTitleTemplate;
            return ReleaseTemplate.Expand(template, context.Entry, context.Version).Trim();
        }
    }
}
