using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

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
        public async Task ExistingReleaseUploadsArchiveWithoutCreatingRelease()
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

                           Assert.AreEqual(HttpMethod.Post, request.Method);
                           StringAssert.StartsWith("https://uploads.github.com/", request.RequestUri.AbsoluteUri);
                           return Response(HttpStatusCode.Created, "{}");
                       })))
            {
                GitHubReleaseResult result = await Publish(client);
                Assert.AreEqual("123", result.ReleaseId);
                StringAssert.EndsWith("/VehicleEnhancements_SN-v1.0.0", result.HtmlUrl);
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

        private Task<GitHubReleaseResult> Publish(GitHubReleasesApiClient client) =>
            client.CreateReleaseWithAssetAsync("mroshaw", "BelowZeroThunderKitMods",
                "VehicleEnhancements_SN-v1.0.0", "Vehicle Enhancements SN v1.0.0", "Change log",
                new GitHubReleaseOptions(), archivePath, new Progress<UploadProgress>(), CancellationToken.None);

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
