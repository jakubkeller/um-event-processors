namespace Services.Interface;

public interface IFileService
{
    Task<byte[]> DownloadFileAsBytesAsync(string url);
}
