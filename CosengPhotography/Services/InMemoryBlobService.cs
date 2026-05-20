    using CosengPhotography.Interfaces;
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

            public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
            {
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";

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

            public Task<string> GetSecureUrlAsync(string blobUrl)
            {
                _logger.LogDebug("Returning secure URL for {BlobUrl}", blobUrl);
                return Task.FromResult(blobUrl);
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