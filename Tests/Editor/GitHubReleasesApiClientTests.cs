using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace DaftAppleGames.Editor.ModPublisher.Tests
{
    public sealed class GitHubReleasesApiClientTests
    {
        private string archivePath;

        [SetUp]
        public void SetUp()
        {
            archivePath = Path.GetTempFileName();
            File.WriteAllText(archivePath, "archive contents");
        }

        [TearDown]
        public void TearDown() => File.Delete(archivePath);

        [Test]
        public async Task ExistingReleaseReplacesDetailsAndAllAssetsWithoutCreatingRelease()
        {
            int requests = 0;
            using (GitHubReleasesApiClient client = new GitHubReleasesApiClient("test-token",
                       new TestHandler(request =>
                       {
                           if (++requests == 1)
                           {
                               AssertReleaseLookup(request);
                               return ReleaseResponse();
                           }

                           if (requests == 2)
                           {
                               AssertReleaseUpdate(request);
                               return ReleaseResponse();
                           }

                           if (requests == 3)
                           {
                               AssertAssetList(request, 1);
                               return Response(HttpStatusCode.OK, new JArray(
                                   new JObject { ["id"] = 456, ["name"] = Path.GetFileName(archivePath) },
                                   new JObject { ["id"] = 789, ["name"] = "old-extra.zip" }).ToString());
                           }

                           if (requests == 4)
                           {
                               Assert.AreEqual(HttpMethod.Delete, request.Method);
                               Assert.AreEqual("https://api.github.com/repos/mroshaw/BelowZeroThunderKitMods/releases/assets/456",
                                   request.RequestUri.AbsoluteUri);
                               return Response(HttpStatusCode.NoContent, "");
                           }

                           if (requests == 5)
                           {
                               Assert.AreEqual(HttpMethod.Delete, request.Method);
                               Assert.AreEqual("https://api.github.com/repos/mroshaw/BelowZeroThunderKitMods/releases/assets/789",
                                   request.RequestUri.AbsoluteUri);
                               return Response(HttpStatusCode.NoContent, "");
                           }

                           Assert.AreEqual(HttpMethod.Post, request.Method);
                           StringAssert.StartsWith("https://uploads.github.com/", request.RequestUri.AbsoluteUri);
                           Assert.AreEqual("archive contents", request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                           return Response(HttpStatusCode.Created, "{}");
                       })))
            {
                GitHubReleaseResult result = await Publish(client);
                Assert.AreEqual("123", result.ReleaseId);
                StringAssert.EndsWith("/VehicleEnhancements_SN-v1.0.0", result.HtmlUrl);
                Assert.AreEqual(6, requests);
            }
        }

        [Test]
        public async Task ExistingReleaseWithoutAssetsUploadsWithoutDeleting()
        {
            int requests = 0;
            using (GitHubReleasesApiClient client = new GitHubReleasesApiClient("test-token",
                       new TestHandler(request =>
                       {
                           requests++;
                           if (requests == 1 || requests == 2) return ReleaseResponse();
                           if (requests == 3)
                           {
                               AssertAssetList(request, 1);
                               return Response(HttpStatusCode.OK, "[]");
                           }

                           Assert.AreEqual(HttpMethod.Post, request.Method);
                           StringAssert.StartsWith("https://uploads.github.com/", request.RequestUri.AbsoluteUri);
                           return Response(HttpStatusCode.Created, "{}");
                       })))
            {
                await Publish(client);
                Assert.AreEqual(4, requests);
            }
        }

        [Test]
        public async Task AllAssetsAcrossPagesAreDeletedBeforeUploading()
        {
            int requests = 0;
            using (GitHubReleasesApiClient client = new GitHubReleasesApiClient("test-token",
                       new TestHandler(request =>
                       {
                           requests++;
                           if (requests == 1 || requests == 2) return ReleaseResponse();
                           if (requests == 3)
                           {
                               AssertAssetList(request, 1);
                               JArray assets = new JArray();
                               for (int i = 0; i < 100; i++)
                               {
                                   assets.Add(new JObject { ["id"] = i, ["name"] = "old-" + i + ".zip" });
                               }

                               return Response(HttpStatusCode.OK, assets.ToString());
                           }

                           if (requests == 4)
                           {
                               AssertAssetList(request, 2);
                               return Response(HttpStatusCode.OK, new JArray(
                                   new JObject { ["id"] = 456, ["name"] = Path.GetFileName(archivePath) }).ToString());
                           }

                           if (requests >= 5 && requests <= 105)
                           {
                               Assert.AreEqual(HttpMethod.Delete, request.Method);
                               int assetId = requests == 105 ? 456 : requests - 5;
                               StringAssert.EndsWith("/releases/assets/" + assetId, request.RequestUri.AbsoluteUri);
                               return Response(HttpStatusCode.NoContent, "");
                           }

                           Assert.AreEqual(HttpMethod.Post, request.Method);
                           StringAssert.StartsWith("https://uploads.github.com/", request.RequestUri.AbsoluteUri);
                           return Response(HttpStatusCode.Created, "{}");
                       })))
            {
                await Publish(client);
                Assert.AreEqual(106, requests);
            }
        }

        [Test]
        public async Task ExistingReleaseGeneratesNotesBeforeUpdating()
        {
            int requests = 0;
            GitHubReleaseOptions options = new GitHubReleaseOptions();
            typeof(GitHubReleaseOptions).GetField("generateReleaseNotes",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(options, true);
            using (GitHubReleasesApiClient client = new GitHubReleasesApiClient("test-token",
                       new TestHandler(request =>
                       {
                           requests++;
                           if (requests == 1) return ReleaseResponse();
                           if (requests == 2)
                           {
                               Assert.AreEqual(HttpMethod.Post, request.Method);
                               StringAssert.EndsWith("/releases/generate-notes", request.RequestUri.AbsoluteUri);
                               return Response(HttpStatusCode.OK, "{\"body\":\"Generated notes\"}");
                           }

                           if (requests == 3)
                           {
                               AssertReleaseUpdate(request, "Change log\n\nGenerated notes");
                               return ReleaseResponse();
                           }

                           if (requests == 4) return Response(HttpStatusCode.OK, "[]");
                           Assert.AreEqual(HttpMethod.Post, request.Method);
                           StringAssert.StartsWith("https://uploads.github.com/", request.RequestUri.AbsoluteUri);
                           return Response(HttpStatusCode.Created, "{}");
                       })))
            {
                await Publish(client, options);
                Assert.AreEqual(5, requests);
            }
        }

        [Test]
        public async Task AssetDeletionFailureStopsUploadAndIdentifiesAsset()
        {
            int requests = 0;
            using (GitHubReleasesApiClient client = new GitHubReleasesApiClient("test-token",
                       new TestHandler(request =>
                       {
                           requests++;
                           if (requests == 1 || requests == 2) return ReleaseResponse();
                           if (requests == 3) return Response(HttpStatusCode.OK, new JArray(
                               new JObject { ["id"] = 456, ["name"] = Path.GetFileName(archivePath) }).ToString());
                           Assert.AreEqual(HttpMethod.Delete, request.Method);
                           return Response(HttpStatusCode.Forbidden, "Delete denied");
                       })))
            {
                HttpRequestException exception = await PublishExpectingFailure(client);
                StringAssert.Contains(Path.GetFileName(archivePath), exception.Message);
                StringAssert.Contains("Delete denied", exception.Message);
                Assert.AreEqual(4, requests);
            }
        }

        [Test]
        public async Task ReleaseUpdateFailureStopsBeforeReplacingAssets()
        {
            int requests = 0;
            using (GitHubReleasesApiClient client = new GitHubReleasesApiClient("test-token",
                       new TestHandler(request =>
                       {
                           if (++requests == 1) return ReleaseResponse();
                           AssertReleaseUpdate(request);
                           return Response(HttpStatusCode.Forbidden, "Update denied");
                       })))
            {
                HttpRequestException exception = await PublishExpectingFailure(client);
                StringAssert.Contains("update the GitHub release", exception.Message);
                StringAssert.Contains("Update denied", exception.Message);
                Assert.AreEqual(2, requests);
            }
        }

        [Test]
        public async Task ReleaseLookupErrorIdentifiesDestinationAndDoesNotCreateOrUpload()
        {
            int requests = 0;
            using (GitHubReleasesApiClient client = new GitHubReleasesApiClient("test-token",
                       new TestHandler(request =>
                       {
                           requests++;
                           AssertReleaseLookup(request);
                           return Response(HttpStatusCode.Forbidden, "Access denied");
                       })))
            {
                HttpRequestException exception = await PublishExpectingFailure(client);
                StringAssert.Contains("mroshaw/BelowZeroThunderKitMods", exception.Message);
                StringAssert.Contains("VehicleEnhancements_SN-v1.0.0", exception.Message);
                StringAssert.Contains("Access denied", exception.Message);
                Assert.AreEqual(1, requests);
            }
        }

        [Test]
        public async Task ReleaseCreationErrorIdentifiesDestinationAndDoesNotUpload()
        {
            int requests = 0;
            using (GitHubReleasesApiClient client = new GitHubReleasesApiClient("test-token",
                       new TestHandler(request =>
                       {
                           if (++requests == 1)
                           {
                               AssertReleaseLookup(request);
                               return Response(HttpStatusCode.NotFound, "{}");
                           }

                           Assert.AreEqual(HttpMethod.Post, request.Method);
                           Assert.AreEqual("https://api.github.com/repos/mroshaw/BelowZeroThunderKitMods/releases",
                               request.RequestUri.AbsoluteUri);
                           return Response((HttpStatusCode)422,
                               "{\"message\":\"Validation Failed\",\"errors\":[{\"resource\":\"Release\",\"code\":\"already_exists\",\"field\":\"tag_name\"}]}");
                       })))
            {
                HttpRequestException exception = await PublishExpectingFailure(client);
                StringAssert.Contains("mroshaw/BelowZeroThunderKitMods", exception.Message);
                StringAssert.Contains("VehicleEnhancements_SN-v1.0.0", exception.Message);
                StringAssert.Contains("already_exists", exception.Message);
                Assert.AreEqual(2, requests);
            }
        }

        [Test]
        public async Task AssetUploadErrorIdentifiesAssetAndDestination()
        {
            int requests = 0;
            using (GitHubReleasesApiClient client = new GitHubReleasesApiClient("test-token",
                       new TestHandler(request => ++requests == 1
                           ? Response(HttpStatusCode.NotFound, "{}")
                           : requests == 2
                               ? ReleaseResponse()
                               : Response(HttpStatusCode.Forbidden, "Upload denied"))))
            {
                HttpRequestException exception = await PublishExpectingFailure(client);
                StringAssert.Contains(Path.GetFileName(archivePath), exception.Message);
                StringAssert.Contains("mroshaw/BelowZeroThunderKitMods", exception.Message);
                StringAssert.Contains("VehicleEnhancements_SN-v1.0.0", exception.Message);
                StringAssert.Contains("Upload denied", exception.Message);
                Assert.AreEqual(3, requests);
            }
        }

        [Test]
        public async Task NewReleaseUploadsArchiveAndReturnsReleaseDetails()
        {
            int requests = 0;
            using (GitHubReleasesApiClient client = new GitHubReleasesApiClient("test-token",
                       new TestHandler(request =>
                       {
                           if (++requests == 1)
                           {
                               AssertReleaseLookup(request);
                               return Response(HttpStatusCode.NotFound, "{}");
                           }

                           if (requests == 2)
                           {
                               Assert.AreEqual(HttpMethod.Post, request.Method);
                               Assert.AreEqual("https://api.github.com/repos/mroshaw/BelowZeroThunderKitMods/releases",
                                   request.RequestUri.AbsoluteUri);
                               return ReleaseResponse();
                           }

                           Assert.AreEqual(HttpMethod.Post, request.Method);
                           Assert.AreEqual("https://uploads.github.com/repos/mroshaw/BelowZeroThunderKitMods/releases/123/assets?name=" +
                                           Uri.EscapeDataString(Path.GetFileName(archivePath)), request.RequestUri.AbsoluteUri);
                           Assert.AreEqual("application/zip", request.Content.Headers.ContentType.MediaType);
                           return Response(HttpStatusCode.Created, "{}");
                       })))
            {
                GitHubReleaseResult result = await Publish(client);
                Assert.AreEqual("123", result.ReleaseId);
                Assert.AreEqual("https://github.com/mroshaw/BelowZeroThunderKitMods/releases/tag/VehicleEnhancements_SN-v1.0.0",
                    result.HtmlUrl);
                Assert.AreEqual(3, requests);
            }
        }

        private Task<GitHubReleaseResult> Publish(GitHubReleasesApiClient client, GitHubReleaseOptions options = null) =>
            client.CreateReleaseWithAssetAsync("mroshaw", "BelowZeroThunderKitMods",
                "VehicleEnhancements_SN-v1.0.0", "Vehicle Enhancements SN v1.0.0", "Change log",
                options ?? new GitHubReleaseOptions(), archivePath, new Progress<UploadProgress>(), CancellationToken.None);

        private async Task<HttpRequestException> PublishExpectingFailure(GitHubReleasesApiClient client)
        {
            try
            {
                await Publish(client);
            }
            catch (HttpRequestException exception)
            {
                return exception;
            }

            Assert.Fail("Expected publishing to fail with an HttpRequestException.");
            return null;
        }

        private static void AssertReleaseLookup(HttpRequestMessage request)
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("https://api.github.com/repos/mroshaw/BelowZeroThunderKitMods/releases/tags/VehicleEnhancements_SN-v1.0.0",
                request.RequestUri.AbsoluteUri);
            Assert.IsNull(request.Content);
        }

        private static void AssertReleaseUpdate(HttpRequestMessage request, string changelog = "Change log")
        {
            Assert.AreEqual("PATCH", request.Method.Method);
            Assert.AreEqual("https://api.github.com/repos/mroshaw/BelowZeroThunderKitMods/releases/123",
                request.RequestUri.AbsoluteUri);
            JObject body = JObject.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            Assert.AreEqual("Vehicle Enhancements SN v1.0.0", (string)body["name"]);
            Assert.AreEqual(changelog, (string)body["body"]);
            Assert.AreEqual(false, (bool)body["draft"]);
            Assert.AreEqual(false, (bool)body["prerelease"]);
            Assert.IsNull(body["generate_release_notes"]);
        }

        private static void AssertAssetList(HttpRequestMessage request, int page)
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("https://api.github.com/repos/mroshaw/BelowZeroThunderKitMods/releases/123/assets?per_page=100&page=" + page,
                request.RequestUri.AbsoluteUri);
        }

        private static HttpResponseMessage ReleaseResponse() => Response(HttpStatusCode.OK,
            "{\"id\":123,\"upload_url\":\"https://uploads.github.com/repos/mroshaw/BelowZeroThunderKitMods/releases/123/assets{?name,label}\",\"html_url\":\"https://github.com/mroshaw/BelowZeroThunderKitMods/releases/tag/VehicleEnhancements_SN-v1.0.0\"}");

        private static HttpResponseMessage Response(HttpStatusCode status, string body) =>
            new HttpResponseMessage(status) { Content = new StringContent(body) };

        private sealed class TestHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> respond;

            public TestHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => this.respond = respond;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken) => Task.FromResult(respond(request));
        }
    }
}
