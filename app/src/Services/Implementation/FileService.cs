using AWS.Lambda.Powertools.Logging;
using Services.Interface;

namespace Services.Implementation;

public class FileService : IFileService
{
    private readonly HttpClient _httpClient;
    public FileService()
    {
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Downloads a file from the specified pre-signed S3 URL and returns its contents as a byte array.
    /// </summary>
    public async Task<byte[]> DownloadFileAsBytesAsync(string url)
    {
        Logger.LogInformation("--- FileService.DownloadFileAsBytesAsync Start ---");

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        var byteData = await response.Content.ReadAsByteArrayAsync();

        Logger.LogDebug("--- FileService.DownloadFileAsBytesAsync Complete ---");

        return byteData;        
    }

}
