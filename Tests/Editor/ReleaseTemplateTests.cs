using NUnit.Framework;

namespace DaftAppleGames.Editor.ModPublisher.Tests
{
    public sealed class ReleaseTemplateTests
    {
        [Test]
        public void DefaultTagTemplateUsesArchiveNameAndVersion()
        {
            string result = ReleaseTemplate.Expand(
                ReleaseTemplate.DefaultTagName,
                "Auto Locker Labels SV",
                "AutoLockerLabels_SN",
                "1.6.3");

            Assert.AreEqual("AutoLockerLabels_SN-v1.6.3", result);
        }

        [Test]
        public void DefaultTitleTemplateUsesDisplayNameAndVersion()
        {
            string result = ReleaseTemplate.Expand(
                ReleaseTemplate.DefaultReleaseTitle,
                "Auto Locker Labels SV",
                "AutoLockerLabels_SN",
                "1.6.3");

            Assert.AreEqual("Auto Locker Labels SV v1.6.3", result);
        }

        [Test]
        public void UnknownTokensArePreservedForForwardCompatibility()
        {
            string result = ReleaseTemplate.Expand(
                "{archiveName}-{channel}-v{version}",
                "Mod",
                "Mod_SN",
                "2.0.0");

            Assert.AreEqual("Mod_SN-{channel}-v2.0.0", result);
        }
    }
}
