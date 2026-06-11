using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Interfaces
{
    public interface IBlobService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, Guid galleryId);
        Task<string> GetSecureUrlAsync(string blobUrl); // Generates the SAS token
        Task DeleteFileAsync (string blobUrl);

    }
}
