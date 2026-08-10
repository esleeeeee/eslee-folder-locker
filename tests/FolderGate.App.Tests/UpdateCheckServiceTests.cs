using FolderGate.App.Services;

namespace FolderGate.App.Tests;

[TestClass]
public sealed class UpdateCheckServiceTests
{
    [TestMethod]
    public void CurrentVersion_IsParseable()
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(UpdateCheckService.CurrentVersion));
        Assert.IsTrue(UpdateCheckService.TryParseVersion(UpdateCheckService.CurrentVersion, out _),
            "The running version must parse so update comparison can work.");
    }

    [TestMethod]
    public void TryParseVersion_AcceptsCommonForms()
    {
        Assert.IsTrue(UpdateCheckService.TryParseVersion("1.2.3", out var plain));
        Assert.AreEqual(new Version(1, 2, 3), plain);

        Assert.IsTrue(UpdateCheckService.TryParseVersion("v1.2.3", out var prefixed));
        Assert.AreEqual(new Version(1, 2, 3), prefixed);

        Assert.IsTrue(UpdateCheckService.TryParseVersion("1.2.3+abc123", out var metadata));
        Assert.AreEqual(new Version(1, 2, 3), metadata);
    }

    [TestMethod]
    public void TryParseVersion_RejectsInvalidAndPrereleaseForms()
    {
        Assert.IsFalse(UpdateCheckService.TryParseVersion(null, out _));
        Assert.IsFalse(UpdateCheckService.TryParseVersion("", out _));
        Assert.IsFalse(UpdateCheckService.TryParseVersion("latest", out _));
        Assert.IsFalse(UpdateCheckService.TryParseVersion("1.2.3-beta", out _),
            "Prerelease-suffixed versions must not take part in stable comparison.");
    }

    [TestMethod]
    public void IsNewer_ComparesNumerically()
    {
        Assert.IsTrue(UpdateCheckService.IsNewer("1.2.2", "1.2.1"));
        Assert.IsTrue(UpdateCheckService.IsNewer("v1.2.2", "1.2.1"));
        Assert.IsTrue(UpdateCheckService.IsNewer("1.10.0", "1.9.9"), "Comparison must be numeric, not lexicographic.");
        Assert.IsFalse(UpdateCheckService.IsNewer("1.2.1", "1.2.1"));
        Assert.IsFalse(UpdateCheckService.IsNewer("1.2.0", "1.2.1"));
        Assert.IsFalse(UpdateCheckService.IsNewer(null, "1.2.1"));
        Assert.IsFalse(UpdateCheckService.IsNewer("garbage", "1.2.1"));
    }

    [TestMethod]
    public void ParseLatestRelease_NewerStableRelease_ReportsUpdate()
    {
        const string json = """{"tag_name":"v9.9.9","draft":false,"prerelease":false}""";

        UpdateCheckResult result = UpdateCheckService.ParseLatestRelease(json, "1.2.1");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("9.9.9", result.LatestVersion);
        Assert.IsTrue(result.IsUpdateAvailable);
    }

    [TestMethod]
    public void ParseLatestRelease_SameVersion_NoUpdate()
    {
        const string json = """{"tag_name":"v1.2.1","draft":false,"prerelease":false}""";

        UpdateCheckResult result = UpdateCheckService.ParseLatestRelease(json, "1.2.1");

        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.IsUpdateAvailable);
    }

    [TestMethod]
    public void ParseLatestRelease_DraftOrPrerelease_IsRejected()
    {
        // /releases/latest excludes both by contract; the parser re-checks anyway.
        Assert.IsFalse(UpdateCheckService.ParseLatestRelease(
            """{"tag_name":"v9.9.9","draft":true,"prerelease":false}""", "1.2.1").Success);
        Assert.IsFalse(UpdateCheckService.ParseLatestRelease(
            """{"tag_name":"v9.9.9","draft":false,"prerelease":true}""", "1.2.1").Success);
    }

    [TestMethod]
    public void ParseLatestRelease_MalformedPayload_FailsSafely()
    {
        Assert.IsFalse(UpdateCheckService.ParseLatestRelease("not json", "1.2.1").Success);
        Assert.IsFalse(UpdateCheckService.ParseLatestRelease("{}", "1.2.1").Success);
        Assert.IsFalse(UpdateCheckService.ParseLatestRelease(
            """{"tag_name":"weird-tag","draft":false,"prerelease":false}""", "1.2.1").Success);
    }
}
