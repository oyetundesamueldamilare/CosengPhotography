using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Interfaces
{
    public interface IBlobService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, Guid galleryId);
        Task<string> GetSecureUrlAsync(string blobKey, string? originalFileName = null); // Generates the SAS token
        Task DeleteFileAsync (string blobUrl);
        Task<Stream> GetFileStreamAsync(string blobKey);

    }
}
