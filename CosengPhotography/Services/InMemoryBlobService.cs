    using CosengPhotography.Interfaces;
using CosengPhotography.Shared.Dtos;
using System.Collections.Concurrent;

    namespace CosengPhotography.Services
    {
        public class InMemoryBlobService : IBlobService
        {
            private readonly ILogger<InMemoryBlobService> _logger;

            // Thread-safe dictionary to hold file content in memory
            private static readonly ConcurrentDictionary<string, byte[]> _store = new();

            public InMemoryBlobService(ILogger<InMemoryBlobService> logger)
            {
                _logger = logger;
            }

            public async Task<string> UploadFileAsync(Stream fileStream, string fileName, Guid galleryId)
            {
             // Simulate a gallery context for unique naming
            var uniqueFileName = $"{galleryId}_{fileName}";

                try
                {
                    using var ms = new MemoryStream();
                    await fileStream.CopyToAsync(ms);

                    // Store file bytes in memory
                    _store[uniqueFileName] = ms.ToArray();

                    _logger.LogInformation("File {FileName} uploaded successfully to in-memory store", fileName);

                    // Return a pseudo-URL (not a real path, just an identifier)
                    return $"inmemory://{uniqueFileName}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload file {FileName}", fileName);
                    throw;
                }
            }
        public Task<Stream> GetFileStreamAsync(string blobKey) 
        {
            //if (_store.TryGetValue(blobKey, out byte[] fileBytes))
            //{
            //    var stream = new MemoryStream(fileBytes);
            //    return Task.FromResult<Stream>(stream);
            //}

            throw new KeyNotFoundException($"File with key {blobKey} not found.");
        }

        public Task<string> GetSecureUrlAsync(string blobKey, string? originalFileName = null)
        {
                _logger.LogDebug("Returning secure URL for {BlobKey}", blobKey);
                return Task.FromResult($"inmemory://{blobKey}");
            }

            public async Task DeleteFileAsync(string blobUrl)
            {
                try
                {
                    var fileName = blobUrl.Replace("inmemory://", string.Empty);

                    if (_store.TryRemove(fileName, out _))
                    {
                        _logger.LogInformation("Deleted file {FileName} from in-memory store", fileName);
                    }
                    else
                    {
                        _logger.LogWarning("File {FileName} not found in in-memory store", fileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file {BlobUrl}", blobUrl);
                }

                await Task.CompletedTask;
            }
        }
    }