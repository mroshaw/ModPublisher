using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace DaftAppleGames.Editor.ModPublisher
{
    public sealed class GitHubReleaseResult
    {
        public GitHubReleaseResult(string releaseId, string htmlUrl)
        {
            ReleaseId = releaseId;
            HtmlUrl = htmlUrl;
        }

        public string ReleaseId { get; }
        public string HtmlUrl { get; }
    }

    public sealed class GitHubReleasesApiClient : IDisposable
    {
        private const string ApiBaseUrl = "https://api.github.com";
        private const string ApiVersion = "2026-03-10";
        private readonly HttpClient client;

        public GitHubReleasesApiClient(string token)
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", ApiVersion);
            client.DefaultRequestHeaders.Add("User-Agent", "DaftAppleGames-ModPublisher");
        }

        public async Task<GitHubReleaseResult> CreateReleaseWithAssetAsync(
            string owner,
            string repository,
            string tagName,
            string releaseTitle,
            string changelog,
            GitHubReleaseOptions options,
            string archivePath,
            IProgress<UploadProgress> progress,
            CancellationToken cancellationToken)
        {
            progress.Report(new UploadProgress(0.05f, "Creating GitHub release..."));
            JObject body = new JObject
            {
                ["tag_name"] = tagName,
                ["name"] = releaseTitle,
                ["body"] = changelog ?? string.Empty,
                ["draft"] = options.Draft,
                ["prerelease"] = options.Prerelease,
                ["generate_release_notes"] = options.GenerateReleaseNotes
            };
            if (!string.IsNullOrWhiteSpace(options.TargetCommitish))
            {
                body["target_commitish"] = options.TargetCommitish.Trim();
            }

            string endpoint = $"/repos/{Escape(owner)}/{Escape(repository)}/releases";
            JObject release = await SendJsonAsync(HttpMethod.Post, ApiBaseUrl + endpoint, body, cancellationToken);
            string releaseId = RequireString(release, "id");
            string uploadUrl = RequireString(release, "upload_url");
            string htmlUrl = RequireString(release, "html_url");
            int templateIndex = uploadUrl.IndexOf('{');
            if (templateIndex >= 0)
            {
                uploadUrl = uploadUrl.Substring(0, templateIndex);
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(new UploadProgress(0.25f, "Uploading GitHub release asset..."));
            string assetName = Path.GetFileName(archivePath);
            string assetUrl = uploadUrl + "?name=" + Uri.EscapeDataString(assetName);
            using (FileStream stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamContent content = new StreamContent(stream))
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, assetUrl))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
                request.Content = content;
                using (HttpResponseMessage response = await client.SendAsync(request, cancellationToken))
                {
                    await EnsureSuccessAsync(response, "upload the GitHub release asset");
                }
            }

            progress.Report(new UploadProgress(1.0f, "GitHub release complete."));
            return new GitHubReleaseResult(releaseId, htmlUrl);
        }

        public void Dispose()
        {
            client.Dispose();
        }

        private async Task<JObject> SendJsonAsync(
            HttpMethod method,
            string url,
            JObject body,
            CancellationToken cancellationToken)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(method, url))
            {
                request.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (HttpResponseMessage response = await client.SendAsync(request, cancellationToken))
                {
                    await EnsureSuccessAsync(response, "create the GitHub release");
                    string json = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(json);
                }
            }
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string responseText = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Failed to {operation}: {(int)response.StatusCode} {response.ReasonPhrase}. {responseText}");
        }

        private static string RequireString(JObject parent, string propertyName)
        {
            JToken token = parent[propertyName];
            string value = token == null ? null : token.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"GitHub response did not contain '{propertyName}'.");
            }

            return value;
        }

        private static string Escape(string value) => Uri.EscapeDataString(value.Trim());
    }
}
