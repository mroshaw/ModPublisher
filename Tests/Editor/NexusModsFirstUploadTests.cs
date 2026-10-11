using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace DaftAppleGames.Editor.ModPublisher.Tests
{
    public sealed class NexusModsFirstUploadTests
    {
        [TestCase(null, "42", "subnauticabelowzero", "", true)]
        [TestCase(null, "", "subnauticabelowzero", "", false)]
        [TestCase(null, "42", "", "", false)]
        [TestCase("existing-file", "", "", "", true)]
        [TestCase("existing-file", "", "subnauticabelowzero", "Release notes", false)]
        public void ValidationRequiresModDetailsForCreationOrChangelog(
            string fileId, string modId, string gameDomain, string changelog, bool expected)
        {
            ModVersionEntry entry = new ModVersionEntry();
            foreach (var field in new[]
                     {
                         new { Name = "fileGroupId", Value = fileId },
                         new { Name = "gameScopedModId", Value = modId },
                         new { Name = "gameDomain", Value = gameDomain }
                     })
            {
                typeof(NexusModsUploadOptions).GetField(field.Name, BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(entry.NexusMods, field.Value);
            }

            ModPublishingContext context = new ModPublishingContext(entry, "archive.zip", changelog,
                "test-key", "", "", "");
            Assert.AreEqual(expected, new NexusModPublishingTarget().TryValidate(context, out _));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task FirstUploadSavesFileIdBeforeChangelog(bool failChangelog)
        {
            NexusModsUploadOptions options = CreateOptions(null);
            bool saved = false;
            FakeNexusHandler handler = new FakeNexusHandler
            {
                FailChangelog = failChangelog,
                BeforeChangelog = () => Assert.IsTrue(saved)
            };
            string archive = Path.GetTempFileName();
            try
            {
                using (NexusModsApiClient client = CreateClient(handler))
                {
                    Task<string> publish = client.UploadNewVersionAsync(
                        options, archive, "1.0.0", "First release", new SilentProgress(),
                        CancellationToken.None, () =>
                        {
                            Assert.AreEqual("new-file", options.FileGroupId);
                            saved = true;
                        });
                    if (failChangelog)
                    {
                        Assert.ThrowsAsync<HttpRequestException>(async () => { await publish; });
                    }
                    else
                    {
                        Assert.AreEqual("new-file", await publish);
                    }
                }

                Assert.IsTrue(saved);
                Assert.AreEqual("new-file", options.FileGroupId);
                FileRequest body = JsonUtility.FromJson<FileRequest>(handler.FileBody);
                Assert.AreEqual("global-mod", body.mod_id);
                Assert.AreEqual("upload", body.upload_id);
                Assert.AreEqual("1.0.0", body.version);
                Assert.IsFalse(handler.FileBody.Contains("\"archive_existing_file\""));
                Assert.AreEqual(1, handler.CreateRequests);
                Assert.AreEqual(0, handler.VersionRequests);
            }
            finally
            {
                File.Delete(archive);
            }
        }

        [TestCase(null, true)]
        [TestCase("existing-file", false)]
        public void FailedPublishDoesNotCreateAFallbackFile(string fileId, bool creatingFile)
        {
            NexusModsUploadOptions options = CreateOptions(fileId);
            FakeNexusHandler handler = new FakeNexusHandler { FailFileRequest = true };
            bool saved = false;
            string archive = Path.GetTempFileName();
            try
            {
                using (NexusModsApiClient client = CreateClient(handler))
                {
                    Assert.ThrowsAsync<HttpRequestException>(async () =>
                    {
                        await client.UploadNewVersionAsync(options, archive, "1.0.0", "",
                            new SilentProgress(), CancellationToken.None, () => saved = true);
                    });
                }

                Assert.IsFalse(saved);
                Assert.AreEqual(fileId, options.FileGroupId);
                Assert.AreEqual(creatingFile ? 1 : 0, handler.CreateRequests);
                Assert.AreEqual(creatingFile ? 0 : 1, handler.VersionRequests);
            }
            finally
            {
                File.Delete(archive);
            }
        }

        [Test]
        public async Task ExistingFilePublishesNewVersionWithoutChangingGroupId()
        {
            NexusModsUploadOptions options = CreateOptions("existing-file");
            FakeNexusHandler handler = new FakeNexusHandler();
            string archive = Path.GetTempFileName();
            try
            {
                using (NexusModsApiClient client = CreateClient(handler))
                {
                    string result = await client.UploadNewVersionAsync(options, archive, "2.0.0", "",
                        new SilentProgress(), CancellationToken.None,
                        () => Assert.Fail("Existing file must not invoke the creation callback."));
                    Assert.AreEqual("new-version", result);
                }

                Assert.AreEqual("existing-file", options.FileGroupId);
                Assert.AreEqual(0, handler.CreateRequests);
                Assert.AreEqual(1, handler.VersionRequests);
                Assert.IsFalse(handler.FileBody.Contains("\"mod_id\""));
                Assert.IsTrue(JsonUtility.FromJson<FileRequest>(handler.FileBody).archive_existing_file);
            }
            finally
            {
                File.Delete(archive);
            }
        }

        private static NexusModsUploadOptions CreateOptions(string fileId)
        {
            NexusModsUploadOptions options = new NexusModsUploadOptions();
            typeof(NexusModsUploadOptions).GetField("fileGroupId", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(options, fileId);
            typeof(NexusModsUploadOptions).GetField("gameScopedModId", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(options, "42");
            return options;
        }

        private static NexusModsApiClient CreateClient(FakeNexusHandler handler)
        {
            NexusModsApiClient client = new NexusModsApiClient("test-key");
            foreach (string fieldName in new[] { "apiClient", "uploadClient" })
            {
                FieldInfo field = typeof(NexusModsApiClient).GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                ((HttpClient)field.GetValue(client)).Dispose();
                field.SetValue(client, new HttpClient(handler, false));
            }
            return client;
        }

        private sealed class SilentProgress : IProgress<UploadProgress>
        {
            public void Report(UploadProgress value) { }
        }

        [Serializable]
        private sealed class FileRequest
        {
            public string mod_id;
            public string upload_id;
            public string version;
            public bool archive_existing_file;
        }

        private sealed class FakeNexusHandler : HttpMessageHandler
        {
            public bool FailChangelog;
            public bool FailFileRequest;
            public Action BeforeChangelog;
            public int CreateRequests;
            public int VersionRequests;
            public string FileBody;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string path = request.RequestUri.AbsolutePath;
                string json;
                if (path == "/v3/games/subnauticabelowzero/mods/42")
                    json = "{\"data\":{\"id\":\"global-mod\"}}";
                else if (path == "/v3/uploads/multipart")
                    json = "{\"data\":{\"id\":\"upload\",\"part_size_bytes\":8,\"part_presigned_urls\":[\"https://storage.test/part\"],\"complete_presigned_url\":\"https://storage.test/complete\"}}";
                else if (path == "/part")
                {
                    HttpResponseMessage part = new HttpResponseMessage(HttpStatusCode.OK);
                    part.Headers.TryAddWithoutValidation("ETag", "\"part-etag\"");
                    return part;
                }
                else if (path == "/complete" || path == "/v3/uploads/upload/finalise")
                    json = "{}";
                else if (path == "/v3/uploads/upload")
                    json = "{\"data\":{\"state\":\"available\"}}";
                else if (path == "/v3/mod-files" || path == "/v3/mod-files/existing-file/versions")
                {
                    bool creating = path == "/v3/mod-files";
                    if (creating) CreateRequests++; else VersionRequests++;
                    FileBody = await request.Content.ReadAsStringAsync();
                    if (FailFileRequest) return new HttpResponseMessage(HttpStatusCode.NotFound);
                    json = creating ? "{\"data\":{\"id\":\"new-file\"}}"
                        : "{\"data\":{\"version\":{\"id\":\"new-version\"}}}";
                }
                else if (path == "/v3/mods/global-mod/changelogs")
                {
                    BeforeChangelog?.Invoke();
                    if (FailChangelog) return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                    json = "{}";
                }
                else
                    throw new InvalidOperationException("Unexpected request: " + request.RequestUri);

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
            }
        }
    }
}
