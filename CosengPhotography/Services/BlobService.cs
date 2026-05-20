using Azure.Storage.Sas;
using Azure.Storage.Blobs;
using CosengPhotography.Interfaces;
using Azure.Storage;


namespace CosengPhotography.Services
{
    public class BlobService : IBlobService
    {
        private readonly string _storagePath;
        private readonly ILogger<BlobService> _logger;

        public BlobService(IConfiguration config, IWebHostEnvironment env, ILogger<BlobService> logger)
        {
            _logger = logger;

            // Get path from config or default to wwwroot/uploads/galleries
            var baseConfigPath = config["FileSettings:StoragePath"];

            _storagePath = !string.IsNullOrEmpty(baseConfigPath)
                ? baseConfigPath
                : Path.Combine(env.WebRootPath, "uploads", "galleries");

            // Ensure directory exists
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
                _logger.LogInformation("Created storage directory at {StoragePath}", _storagePath);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(_storagePath, uniqueFileName);

            try
            {
                using (var localFileStream = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(localFileStream);
                }

                _logger.LogInformation("File {FileName} uploaded successfully to {FilePath}", fileName, filePath);

                return $"/uploads/galleries/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName}", fileName);
                throw; // rethrow so controller can handle it
            }
        }

        public Task<string> GetSecureUrlAsync(string blobUrl)
        {
            _logger.LogDebug("Returning secure URL for {BlobUrl}", blobUrl);
            return Task.FromResult(blobUrl);
        }

        public async Task DeleteFileAsync(string blobUrl)
        {
            try
            {
                var fileName = Path.GetFileName(blobUrl);
                var filePath = Path.Combine(_storagePath, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted file {FilePath}", filePath);
                }
                else
                {
                    _logger.LogWarning("File {FilePath} not found for deletion", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {BlobUrl}", blobUrl);
            }

            await Task.CompletedTask;
        }

        //public string GetSasToken(string containerName, string blobName, TimeSpan validFor)
        //{
        //    var blobServiceClient = new BlobServiceClient(_connectionString);
        //    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        //    var blobClient = containerClient.GetBlobClient(blobName);

        //    var sasBuilder = new BlobSasBuilder
        //    {
        //        BlobContainerName = containerName,
        //        BlobName = blobName,
        //        Resource = "b", // b = blob
        //        ExpiresOn = DateTimeOffset.UtcNow.Add(validFor)
        //    };

        //    sasBuilder.SetPermissions(BlobSasPermissions.Read);

        //    var sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(
        //        _accountName, _accountKey)).ToString();

        //    return $"{blobClient.Uri}?{sasToken}";
        //}

    }
}
