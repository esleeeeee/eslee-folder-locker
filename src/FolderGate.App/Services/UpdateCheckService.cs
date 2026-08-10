using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace FolderGate.App.Services;

public sealed record UpdateCheckResult(bool Success, string? LatestVersion, bool IsUpdateAvailable, string? Error);

/// <summary>
/// Compares the running version against the latest stable GitHub release.
///
/// This is the app's only network access: a single anonymous GET to the GitHub
/// releases API, carrying no user data. The <c>/releases/latest</c> endpoint
/// excludes drafts and prereleases by contract, and the parser re-checks both
/// flags defensively. Any failure (offline, rate limit, bad payload) is
/// reported as an unsuccessful result — callers log it quietly and locking
/// behavior is never affected. There is no auto-update: users are pointed to
/// the release page to download the installer themselves.
/// </summary>
public sealed class UpdateCheckService
{
    public const string ReleasesPageUrl = "https://github.com/esleeeeee/eslee-folder-locker/releases/latest";
    private const string LatestReleaseApiUrl = "https://api.github.com/repos/esleeeeee/eslee-folder-locker/releases/latest";

    /// <summary>Startup checks run at most once per this interval.</summary>
    public static readonly TimeSpan StartupCheckInterval = TimeSpan.FromHours(24);

    private static readonly HttpClient Http = CreateClient();

    public static string CurrentVersion { get; } = ResolveCurrentVersion();

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await Http.GetAsync(LatestReleaseApiUrl, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(false, null, false, $"HTTP {(int)response.StatusCode}");
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ParseLatestRelease(json, CurrentVersion);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or IOException)
        {
            return new UpdateCheckResult(false, null, false, ex.Message);
        }
    }

    /// <summary>Pure parsing/comparison step, separated for unit testing.</summary>
    public static UpdateCheckResult ParseLatestRelease(string json, string currentVersion)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            if ((root.TryGetProperty("draft", out JsonElement draft) && draft.ValueKind == JsonValueKind.True) ||
                (root.TryGetProperty("prerelease", out JsonElement prerelease) && prerelease.ValueKind == JsonValueKind.True))
            {
                return new UpdateCheckResult(false, null, false, "latest release is draft/prerelease");
            }

            string? tag = root.TryGetProperty("tag_name", out JsonElement tagElement) ? tagElement.GetString() : null;
            if (!TryParseVersion(tag, out _))
            {
                return new UpdateCheckResult(false, null, false, "unrecognized tag");
            }

            string latest = NormalizeVersionText(tag!);
            return new UpdateCheckResult(true, latest, IsNewer(latest, currentVersion), null);
        }
        catch (JsonException ex)
        {
            return new UpdateCheckResult(false, null, false, ex.Message);
        }
    }

    /// <summary>True only when both versions parse and latest is strictly newer.</summary>
    public static bool IsNewer(string? latest, string? current)
    {
        return TryParseVersion(latest, out Version? latestVersion) &&
               TryParseVersion(current, out Version? currentVersion) &&
               latestVersion! > currentVersion!;
    }

    /// <summary>
    /// Accepts "1.2.3", "v1.2.3", and values carrying "+metadata" suffixes.
    /// Prerelease suffixes like "-beta" are rejected on purpose: only stable
    /// versions take part in the comparison.
    /// </summary>
    public static bool TryParseVersion(string? text, out Version? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string cleaned = NormalizeVersionText(text);
        if (Version.TryParse(cleaned, out Version? parsed))
        {
            version = parsed;
            return true;
        }

        return false;
    }

    private static string NormalizeVersionText(string text)
    {
        string cleaned = text.Trim();
        if (cleaned.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[1..];
        }

        int metadataStart = cleaned.IndexOf('+');
        if (metadataStart >= 0)
        {
            cleaned = cleaned[..metadataStart];
        }

        return cleaned;
    }

    private static string ResolveCurrentVersion()
    {
        Assembly assembly = typeof(UpdateCheckService).Assembly;
        string? informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return NormalizeVersionText(informational);
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    private static HttpClient CreateClient()
    {
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        // GitHub's API rejects requests without a User-Agent. Kept version-free so
        // this initializer has no ordering dependency on CurrentVersion.
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("eslee-folder-locker-update-check", "1"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
}
