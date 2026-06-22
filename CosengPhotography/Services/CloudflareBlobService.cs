using CosengPhotography.Interfaces;
using Microsoft.AspNetCore.StaticFiles;
using System.Net.Http.Headers;

namespace CosengPhotography.Services
{
    public class CloudflareBlobService : IBlobService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CloudflareBlobService> _logger;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;

        private readonly string _bucketUrl;
        private readonly string _apiToken;
        private readonly string _publicDomainPrefix;

        public CloudflareBlobService(HttpClient httpClient, IConfiguration configuration, ILogger<CloudflareBlobService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _contentTypeProvider = new FileExtensionContentTypeProvider();

            var accountId = configuration["Cloudflare:AccountId"]
                ?? throw new ArgumentNullException(nameof(configuration), "Cloudflare AccountId is missing from configurations.");
            var bucketName = configuration["Cloudflare:BucketName"]
                ?? throw new ArgumentNullException(nameof(configuration), "Cloudflare BucketName is missing from configurations.");

            _apiToken = configuration["Cloudflare:ApiToken"]
                ?? throw new ArgumentNullException(nameof(configuration), "Cloudflare ApiToken is missing from configurations.");

            _publicDomainPrefix = configuration["Cloudflare:PublicDomainPrefix"] ?? "";

            // Base endpoint for Cloudflare R2 Workers/REST API operational mapping
            _bucketUrl = $"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}/objects";
        }

        /// <summary>
        /// Streams a photographer's uploaded file straight into your Cloudflare R2 container space using native HTTP PUT.
        /// </summary>

        public async Task<Stream> GetFileStreamAsync(string blobKey)
        {
            string requestUrl = $"{_bucketUrl}/{Uri.EscapeDataString(blobKey)}";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Caller must dispose the returned stream
            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, Guid galleryId)
        {
            if (fileStream == null) throw new ArgumentNullException(nameof(fileStream));
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Filename cannot be blank.", nameof(fileName));

            if (fileStream.CanSeek && fileStream.Position != 0)
            {
                fileStream.Position = 0;
            }

            // Build the predictable lookup composite key format
            string uniqueKey = $"{galleryId}_{Path.GetFileName(fileName)}";
            string requestUrl = $"{_bucketUrl}/{Uri.EscapeDataString(uniqueKey)}";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, requestUrl);

                // Credentials authorization
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

                // Streaming implementation optimized for larger multi-part files
                request.Content = new StreamContent(fileStream);

                // FIXED: Resolve the Mime Type dynamically based on file format extension context
                if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
                {
                    contentType = "application/octet-stream"; // Safe raw binary fallback
                }
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                // Use HttpCompletionOption.ResponseHeadersRead to minimize memory footprint allocations
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Asset structural matrix written successfully to Cloudflare R2. Key: {Key}", uniqueKey);

                return uniqueKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName} to Cloudflare R2 via REST API infrastructure", fileName);
                throw;
            }
        }

        /// <summary>
        /// Generates an absolute asset access path routing via your Cloudflare edge pipeline.
        /// If an original name is supplied, it provides a secure download link with download forcing headers.
        /// </summary>
        public async Task<string> GetSecureUrlAsync(string blobKey, string? originalFileName = null)
        {
            if (string.IsNullOrEmpty(blobKey)) throw new ArgumentNullException(nameof(blobKey));

            _logger.LogDebug("Resolving asset delivery route for Cloudflare Object Key: {Key}", blobKey);

            try
            {
                // Clean the blob key if a full URL path accidentally gets slipped into parameters
                string cleanKey = blobKey.Contains("/") ? Path.GetFileName(blobKey) : blobKey;

                // APPROACH A: If a clean presentation filename is present, generate download disposition headers
                if (!string.IsNullOrEmpty(originalFileName))
                {
                    // If you are using Cloudflare Workers or pre-signed URL parameter mechanisms, override here.
                    // For typical secure CDN endpoints passing query overrides:
                    string downloadCdnPath = $"{_publicDomainPrefix.TrimEnd('/')}/{Uri.EscapeDataString(cleanKey)}?response-content-disposition=attachment;filename=\"{Uri.EscapeDataString(originalFileName)}\"";
                    return await Task.FromResult(downloadCdnPath);
                }

                // APPROACH B: Standard asset pathway mapping for loading/viewing directly within Blazor <img> grids
                if (!string.IsNullOrEmpty(_publicDomainPrefix))
                {
                    return await Task.FromResult($"{_publicDomainPrefix.TrimEnd('/')}/{Uri.EscapeDataString(cleanKey)}");
                }

                return await Task.FromResult($"{_bucketUrl}/{Uri.EscapeDataString(cleanKey)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build Cloudflare asset path route for key {Key}", blobKey);
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
                // Ensure we parse out just the base unique key name string safely
                string targetKey = blobUrlOrKey.Contains("/") ? Path.GetFileName(blobUrlOrKey) : blobUrlOrKey;
                string requestUrl = $"{_bucketUrl}/{Uri.EscapeDataString(targetKey)}";

                using var request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Successfully dropped object key {Key} from Cloudflare infrastructure records.", targetKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to purge file key {BlobUrlOrKey} from Cloudflare storage engine contexts.", blobUrlOrKey);
                throw;
            }
        }
    }
}