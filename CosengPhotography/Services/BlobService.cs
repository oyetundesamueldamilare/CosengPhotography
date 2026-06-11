using Amazon.S3;
using Amazon.S3.Model;
using CosengPhotography.Interfaces;
using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Services
{
    public class BlobService : IBlobService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly ILogger<BlobService> _logger;

        public BlobService(IAmazonS3 s3Client, IConfiguration configuration, ILogger<BlobService> logger)
        {
            _s3Client = s3Client;
            _logger = logger;
            _bucketName = configuration["AWS:BucketName"]
                ?? throw new ArgumentNullException("AWS BucketName configuration is missing from appsettings.");
        }

        /// <summary>
        /// Streams a photographer's uploaded file straight into your Amazon S3 Bucket container space.
        /// </summary>
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName,Guid galleryId)
        {
            // Ensure the incoming network stream index pointer is set to the beginning
            if (fileStream.CanSeek && fileStream.Position != 0)
            {
                fileStream.Position = 0;
            }

            // Create a unique object key prefix to prevent file naming namespace collisions in S3
            string uniqueKey = $"{galleryId}_{Path.GetFileName(fileName)}";

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = uniqueKey,
                    InputStream = fileStream,
                    AutoCloseStream = false // Keeps the lifecycle stream open for batch handling operations
                };

                // Dynamic access control assignment:
                // Pre-watermarked files for public/client grids are made readable to the browser.
                // If you ever upload hidden/raw files later, you can conditionally flag them as Private.
                putRequest.CannedACL = S3CannedACL.PublicRead;

                await _s3Client.PutObjectAsync(putRequest);
                _logger.LogInformation("File {FileName} uploaded successfully to S3 bucket with Key: {Key}", fileName, uniqueKey);

                // Return the raw S3 storage object key to save into your database row field
                return uniqueKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName} to Amazon S3", fileName);
                throw;
            }
        }

        /// <summary>
        /// Generates an encrypted, temporary pre-signed URL for high-res asset access windows.
        /// </summary>
        public async Task<string> GetSecureUrlAsync(string blobKey, string? originalFileName = null)
        {
            _logger.LogDebug("Generating secure pre-signed URL window for S3 Object Key: {Key}", blobKey);

            try
            {
                var expiryWindow = DateTime.UtcNow.AddMinutes(20); // Link automatically self-destructs in 20 minutes

                var urlRequest = new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = blobKey,
                    Expires = expiryWindow,
                    Verb = HttpVerb.GET
                };

                // Cryptographically signs an authentication string onto the file request URL
                string preSignedUrl = await Task.Run(() => _s3Client.GetPreSignedURL(urlRequest));
                return preSignedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate a pre-signed S3 download URL for key {Key}", blobKey);
                throw;
            }
        }

        public Task<Stream> GetFileStreamAsync(string blobKey)
        {
            try
            {
                var getRequest = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = blobKey
                };
                var response = _s3Client.GetObjectAsync(getRequest).Result;
                return Task.FromResult(response.ResponseStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve file stream for S3 object key {Key}", blobKey);
                throw;
            }
        }
        /// <summary>
        /// Removes the targeted object file path permanently from your active cloud bucket.
        /// </summary>
        public async Task DeleteFileAsync(string blobUrlOrKey)
        {
            if (string.IsNullOrEmpty(blobUrlOrKey)) return;

            try
            {
                // Isolate the raw key from any absolute URL path traces if present
                var s3Key = Path.GetFileName(blobUrlOrKey);

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = s3Key
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);
                _logger.LogInformation("Successfully deleted S3 object key {Key} from bucket {Bucket}", s3Key, _bucketName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to purge file {BlobUrlOrKey} from Amazon S3 infrastructure logs", blobUrlOrKey);
            }
        }
    }
}
