namespace CosengPhotography.Interfaces
{
    public interface IBlobService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName);
        Task<string> GetSecureUrlAsync(string blobUrl); // Generates the SAS token
        Task DeleteFileAsync (string blobUrl);

    }
}
