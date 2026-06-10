using System.Net.Http.Headers;
using CosengPhotography.Interfaces;

namespace CosengPhotography.Services
{
    public class CloudflareBlobService : IBlobService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CloudflareBlobService> _logger;
        private readonly string _bucketUrl;
        private readonly string _apiToken;
        private readonly string _publicDomainPrefix;

        public CloudflareBlobService(HttpClient httpClient, IConfiguration configuration, ILogger<CloudflareBlobService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var accountId = configuration["Cloudflare:AccountId"]
                ?? throw new ArgumentNullException("Cloudflare AccountId is missing from configurations.");
            var bucketName = configuration["Cloudflare:BucketName"]
                ?? throw new ArgumentNullException("Cloudflare BucketName is missing from configurations.");

            _apiToken = configuration["Cloudflare:ApiToken"]
                ?? throw new ArgumentNullException("Cloudflare ApiToken is missing from configurations.");

            // Base public CDN or R2 dev subdomain URL for reading files (e.g., https://pub-xyz.r2.dev)
            _publicDomainPrefix = configuration["Cloudflare:PublicDomainPrefix"] ?? "";

            // Target endpoint for direct REST mutations on Cloudflare storage infrastructure
            _bucketUrl = $"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}/objects";
        }

        /// <summary>
        /// Streams a photographer's uploaded file straight into your Cloudflare R2 container space using native HTTP PUT.
        /// </summary>
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
        {
            if (fileStream.CanSeek && fileStream.Position != 0)
            {
                fileStream.Position = 0;
            }

            string uniqueKey = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
            string requestUrl = $"{_bucketUrl}/{uniqueKey}";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, requestUrl);

                // Authorize using Cloudflare API Token credentials
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

                // Attach raw streaming bytes directly to the outgoing body content context
                request.Content = new StreamContent(fileStream);

                // Map the content header dynamically based on standard image signatures
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("File {FileName} uploaded successfully to Cloudflare R2 with Key: {Key}", fileName, uniqueKey);

                return uniqueKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName} to Cloudflare R2 via REST API", fileName);
                throw;
            }
        }

        /// <summary>
        /// Generates an absolute asset access path routing via your Cloudflare edge pipeline.
        /// </summary>
        public async Task<string> GetSecureUrlAsync(string blobUrlOrKey)
        {
            _logger.LogDebug("Resolving asset delivery route for Cloudflare Object Key: {Key}", blobUrlOrKey);

            try
            {
                // If you leverage Cloudflare's tokenized access rules or a public dev domain setup:
                if (!string.IsNullOrEmpty(_publicDomainPrefix))
                {
                    string absoluteCdnPath = $"{_publicDomainPrefix.TrimEnd('/')}/{blobUrlOrKey}";
                    return await Task.FromResult(absoluteCdnPath);
                }

                // Fallback direct endpoint string if routing directly via the raw storage footprint
                return await Task.FromResult($"{_bucketUrl}/{blobUrlOrKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build Cloudflare asset path route for key {Key}", blobUrlOrKey);
                throw;
            }
        }

        /// <summary>
        /// Removes the targeted object file path permanently from your active Cloudflare bucket via HTTP DELETE.
        /// </summary>
        public async Task DeleteFileAsync(string blobUrlOrKey)
        {
            if (string.IsNullOrEmpty(blobUrlOrKey)) return;

            try
            {
                var targetKey = Path.GetFileName(blobUrlOrKey);
                string requestUrl = $"{_bucketUrl}/{targetKey}";

                using var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Successfully deleted Cloudflare object key {Key} from bucket infrastructure", targetKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to purge file {BlobUrlOrKey} from Cloudflare R2 infrastructure logs", blobUrlOrKey);
            }
        }
    }
}