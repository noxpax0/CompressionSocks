using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace CompressionGarmentOrder;

internal static class UpdateManager
{
    private const string RepositoryOwner = "noxpax0";
    private const string RepositoryName = "CompressionSocks";
    private const string ExecutableAssetName = "Mercury.exe";
    private const string ChecksumAssetName = "Mercury.exe.sha256";
    private const string ApplyUpdateArgument = "--apply-update";
    private static readonly Uri LatestReleaseUri = new("https://api.github.com/repos/noxpax0/CompressionSocks/releases/latest");

    public static bool HandleUpdaterMode(string[] args)
    {
        if (args.Length < 5 || !string.Equals(args[1], ApplyUpdateArgument, StringComparison.OrdinalIgnoreCase))
            return false;

        ApplicationConfiguration.Initialize();
        string targetPath = Path.GetFullPath(args[2]);
        if (!int.TryParse(args[3], NumberStyles.None, CultureInfo.InvariantCulture, out int oldProcessId))
        {
            ShowUpdateError("The update could not identify the running Mercury process.");
            return true;
        }

        try
        {
            WaitForProcessToExit(oldProcessId, TimeSpan.FromSeconds(25));
            ReplaceWithRetries(Environment.ProcessPath!, targetPath, TimeSpan.FromSeconds(20));
            var restart = new ProcessStartInfo(targetPath)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(targetPath) ?? AppContext.BaseDirectory
            };
            restart.ArgumentList.Add("--updated-to");
            restart.ArgumentList.Add(args[4]);
            restart.ArgumentList.Add(Environment.ProcessPath!);
            Process.Start(restart);
        }
        catch (Exception ex)
        {
            ShowUpdateError("Mercury could not finish installing the update. Your existing version was left in place.\n\n" + ex.Message);
        }

        return true;
    }

    public static void ScheduleCleanupAfterUpdate(string[] args)
    {
        if (args.Length < 4 || !string.Equals(args[1], "--updated-to", StringComparison.OrdinalIgnoreCase))
            return;

        string updatesRoot = Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Mercury", "Updates"));
        string stagedExecutable;

        try
        {
            stagedExecutable = Path.GetFullPath(args[3]);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return;
        }

        string trustedPrefix = updatesRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!stagedExecutable.StartsWith(trustedPrefix, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetFileName(stagedExecutable), ExecutableAssetName, StringComparison.OrdinalIgnoreCase))
            return;

        _ = Task.Run(async () =>
        {
            await Task.Delay(1800);
            for (int attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    string? versionDirectory = Path.GetDirectoryName(stagedExecutable);
                    File.Delete(stagedExecutable);
                    if (versionDirectory is not null)
                        Directory.Delete(versionDirectory, false);
                    if (Directory.Exists(updatesRoot) && !Directory.EnumerateFileSystemEntries(updatesRoot).Any())
                        Directory.Delete(updatesRoot, false);
                    return;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }

                await Task.Delay(500);
            }
        });
    }
    public static async Task CheckAndApplyAsync(Form owner)
    {
        try
        {
            await Task.Delay(2600);
            if (owner.IsDisposed || !owner.IsHandleCreated)
                return;

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var client = CreateClient();
            using HttpResponseMessage response = await client.GetAsync(LatestReleaseUri, HttpCompletionOption.ResponseHeadersRead, cancellation.Token);
            if (!response.IsSuccessStatusCode)
                return;

            await using Stream jsonStream = await response.Content.ReadAsStreamAsync(cancellation.Token);
            using JsonDocument document = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellation.Token);
            JsonElement release = document.RootElement;
            string? tag = release.TryGetProperty("tag_name", out JsonElement tagElement) ? tagElement.GetString() : null;
            if (!TryParseReleaseVersion(tag, out Version? availableVersion) || availableVersion is null)
                return;

            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            if (availableVersion <= currentVersion)
                return;

            if (!TryFindAsset(release, ExecutableAssetName, out Uri? executableUri) ||
                !TryFindAsset(release, ChecksumAssetName, out Uri? checksumUri) ||
                executableUri is null || checksumUri is null ||
                !IsTrustedReleaseAsset(executableUri) || !IsTrustedReleaseAsset(checksumUri))
                return;

            string updateDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Mercury", "Updates", availableVersion.ToString(3));
            Directory.CreateDirectory(updateDirectory);
            string stagedPath = Path.Combine(updateDirectory, ExecutableAssetName);
            string partialPath = stagedPath + ".download";

            string checksumText = await client.GetStringAsync(checksumUri, cancellation.Token);
            string expectedHash = checksumText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            if (!IsSha256(expectedHash))
                return;

            using (HttpResponseMessage download = await client.GetAsync(executableUri, HttpCompletionOption.ResponseHeadersRead, cancellation.Token))
            {
                download.EnsureSuccessStatusCode();
                await using Stream source = await download.Content.ReadAsStreamAsync(cancellation.Token);
                await using var destination = new FileStream(
                    partialPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                await source.CopyToAsync(destination, cancellation.Token);
                await destination.FlushAsync(cancellation.Token);
            }

            string actualHash;
            await using (var file = new FileStream(
                partialPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                actualHash = Convert.ToHexString(await SHA256.HashDataAsync(file, cancellation.Token));
            }

            if (!FixedTimeEqualsHex(expectedHash, actualHash))
            {
                File.Delete(partialPath);
                return;
            }

            File.Move(partialPath, stagedPath, true);
            string currentExecutable = Environment.ProcessPath ?? throw new InvalidOperationException("Mercury could not locate its executable.");
            var updater = new ProcessStartInfo(stagedPath)
            {
                UseShellExecute = false,
                WorkingDirectory = updateDirectory
            };
            updater.ArgumentList.Add(ApplyUpdateArgument);
            updater.ArgumentList.Add(currentExecutable);
            updater.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
            updater.ArgumentList.Add(availableVersion.ToString(3));
            Process.Start(updater);
            owner.BeginInvoke((System.Windows.Forms.MethodInvoker)Application.Exit);
        }
        catch (OperationCanceledException) { }
        catch (HttpRequestException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (JsonException) { }
        catch (InvalidOperationException) { }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mercury", GetCurrentVersion()));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private static string GetCurrentVersion()
    {
        Version version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
        return version.ToString(3);
    }

    private static bool TryFindAsset(JsonElement release, string name, out Uri? uri)
    {
        uri = null;
        if (!release.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
            return false;

        foreach (JsonElement asset in assets.EnumerateArray())
        {
            string? assetName = asset.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() : null;
            string? downloadUrl = asset.TryGetProperty("browser_download_url", out JsonElement urlElement) ? urlElement.GetString() : null;
            if (string.Equals(assetName, name, StringComparison.Ordinal) &&
                Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? parsed))
            {
                uri = parsed;
                return true;
            }
        }

        return false;
    }

    private static bool IsTrustedReleaseAsset(Uri uri)
    {
        string expectedPrefix = $"/{RepositoryOwner}/{RepositoryName}/releases/download/";
        return uri.Scheme == Uri.UriSchemeHttps &&
               string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) &&
               uri.AbsolutePath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseReleaseVersion(string? tag, out Version? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(tag))
            return false;

        string value = tag.Trim().TrimStart('v', 'V');
        int suffix = value.IndexOfAny(['-', '+']);
        if (suffix >= 0)
            value = value[..suffix];
        return Version.TryParse(value, out version);
    }

    private static bool IsSha256(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);

    private static bool FixedTimeEqualsHex(string expected, string actual)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expected), Convert.FromHexString(actual));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void WaitForProcessToExit(int processId, TimeSpan timeout)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
                throw new TimeoutException("The previous Mercury process did not close in time.");
        }
        catch (ArgumentException) { }
    }

    private static void ReplaceWithRetries(string sourcePath, string targetPath, TimeSpan timeout)
    {
        string targetDirectory = Path.GetDirectoryName(targetPath)
            ?? throw new InvalidOperationException("The Mercury installation folder could not be located.");
        string stagedReplacement = Path.Combine(
            targetDirectory,
            $".{Path.GetFileName(targetPath)}.{Environment.ProcessId}.update");

        File.Copy(sourcePath, stagedReplacement, true);
        Exception? lastError = null;
        Stopwatch timer = Stopwatch.StartNew();

        while (timer.Elapsed < timeout)
        {
            try
            {
                File.Move(stagedReplacement, targetPath, true);
                return;
            }
            catch (IOException ex) { lastError = ex; }
            catch (UnauthorizedAccessException ex) { lastError = ex; }
            Thread.Sleep(300);
        }

        try { File.Delete(stagedReplacement); } catch { }
        throw new IOException("The executable could not be replaced.", lastError);
    }

    private static void ShowUpdateError(string message) =>
        MessageBox.Show(message, "Mercury Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
}