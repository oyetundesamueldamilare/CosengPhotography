using CosengPhotography.Interfaces;
using CosengPhotography.Models;
using CosengPhotography.Shared.Dtos;
using CosengPhotography.Data;
using Microsoft.EntityFrameworkCore;

namespace CosengPhotography.Services
{
    public class GalleryBackgroundWorker : BackgroundService
    {
        private readonly IGalleryTaskQueue _taskQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GalleryBackgroundWorker> _logger;

        public GalleryBackgroundWorker(
            IGalleryTaskQueue taskQueue,
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<GalleryBackgroundWorker> logger)
        {
            _taskQueue = taskQueue;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Gallery Background Processing Queue Service is running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var payload = await _taskQueue.DequeueTaskAsync(stoppingToken);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        // Inject IBlobService to handle cloud deletions
                        var blobService = scope.ServiceProvider.GetRequiredService<IBlobService>();

                        _logger.LogInformation("Background worker processing database entries for gallery {GalleryId}", payload.GalleryId);

                        // 1. Process Uploads if new photos exist in the payload
                        if (payload.Photos != null && payload.Photos.Any())
                        {
                            var photoEntities = payload.Photos.Select(p => new Photo
                            {
                                GalleryId = payload.GalleryId,
                                BlobUrl = p.BlobUrl,
                                FileName = p.FileName,
                                FileSize = p.FileSize,
                                UploadedAt = DateTime.UtcNow
                            }).ToList();

                            await context.Photos.AddRangeAsync(photoEntities, stoppingToken);
                            await context.SaveChangesAsync(stoppingToken);
                        }
                        // -------------------------------------------------------------
                        // NEW ADDITION: AUTOMATIC CLOUD CLEANUP
                        // If a payload arrives EMPTY (meaning no new photos), it signifies 
                        // a deletion tracking command or an internal synchronization task.
                        // -------------------------------------------------------------
                        else
                        {
                            _logger.LogInformation("No new photos provided. Checking cloud storage for orphaned files linked to Gallery: {GalleryId}", payload.GalleryId);

                            // Look for any remaining tracking records or find file paths linked to the ID
                            var filesToDelete = await context.Photos
                                .Where(p => p.GalleryId == payload.GalleryId)
                                .Select(p => p.BlobUrl)
                                .ToListAsync(stoppingToken);

                            if (filesToDelete.Any())
                            {
                                _logger.LogWarning("Found {Count} files in cloud storage to delete.", filesToDelete.Count);
                                foreach (var url in filesToDelete)
                                {
                                    // Extract the key filename from the full URL string 
                                    string fileKey = Path.GetFileName(url);

                                    // Completely purge it from Cloudflare R2 / AWS S3
                                    await blobService.DeleteFileAsync(fileKey);
                                }
                            }
                        }

                        // 2. Fetch fresh gallery details to build your email notifications
                        var gallery = await context.Galleries.FindAsync(new object[] { payload.GalleryId }, cancellationToken: stoppingToken);

                        if (gallery != null)
                        {
                            string baseUrl = _configuration["FrontendBaseUrl"] ?? "https://localhost:7111";
                            baseUrl = baseUrl.TrimEnd('/');

                            var emailNotification = new GalleryNotificationDto
                            {
                                CustomerEmail = gallery.CustomerEmail,
                                EventName = gallery.EventName,
                                AccessPin = gallery.AccessPin,
                                ShareUrl = $"{baseUrl}/view/{payload.GalleryId}"
                            };

                            _logger.LogInformation("Dispatching notification mail to customer: {Email} with link: {Url}", gallery.CustomerEmail, emailNotification.ShareUrl);
                            await emailService.SendGalleryAccessEmailAsync(emailNotification);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Catch block to cleanly allow system shutdown operations
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unhandled error crushed a background tracking processing channel block.");
                }
            }
        }
    }
}