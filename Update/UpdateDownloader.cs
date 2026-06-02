using System.Net.Http;
using System.Security.Cryptography;

namespace VoogleRoute.Update;

internal static class UpdateDownloader
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(3)
    };

    internal static async Task<DownloadResult> DownloadAsync(string url, string destinationPath, string? expectedSha256Hex)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            var tempPath = destinationPath + ".download";
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* ignored */ }
            }

            var bytes = await Http.GetByteArrayAsync(url).ConfigureAwait(false);
            await File.WriteAllBytesAsync(tempPath, bytes).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(expectedSha256Hex))
            {
                var hash = Convert.ToHexString(SHA256.HashData(bytes));
                if (!hash.Equals(expectedSha256Hex.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(tempPath); } catch { /* ignored */ }
                    return DownloadResult.Fail("Download integrity check failed (SHA-256 mismatch).");
                }
            }

            if (File.Exists(destinationPath))
            {
                try { File.Delete(destinationPath); } catch { /* ignored */ }
            }

            File.Move(tempPath, destinationPath, overwrite: true);
            return DownloadResult.Ok();
        }
        catch (Exception ex)
        {
            return DownloadResult.Fail(ex.Message);
        }
    }

    internal readonly struct DownloadResult
    {
        public bool Success { get; }
        public string? Error { get; }

        private DownloadResult(bool success, string? error)
        {
            Success = success;
            Error = error;
        }

        public static DownloadResult Ok() => new(true, null);

        public static DownloadResult Fail(string error) => new(false, error);
    }
}
