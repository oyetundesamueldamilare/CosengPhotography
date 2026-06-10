using CosengPhotography.Interfaces;
using CosengPhotography.Models;
using CosengPhotography.Shared.Dtos;
using CosengPhotography.Data;

namespace CosengPhotography.Services
{
    public class GalleryBackgroundWorker : BackgroundService
    {
        private readonly IGalleryTaskQueue _taskQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GalleryBackgroundWorker> _logger;

        public GalleryBackgroundWorker(
            IGalleryTaskQueue taskQueue,
            IServiceProvider serviceProvider,
            ILogger<GalleryBackgroundWorker> logger)
        {
            _taskQueue = taskQueue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Gallery Background Processing Queue Service is running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Asynchronously blocks execution thread until a job lands in the channel
                    var payload = await _taskQueue.DequeueTaskAsync(stoppingToken);

                    // Create an isolated Dependency Injection Scope to fetch DbContext & Email safely
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        _logger.LogInformation("Background worker processing database entries for gallery {GalleryId}", payload.GalleryId);

                        // 1. Map pay-load metadata back to explicit Photo Entities
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

                        // 2. Fetch fresh gallery details to build your email notifications
                        var gallery = await context.Galleries.FindAsync(new object[] { payload.GalleryId }, cancellationToken: stoppingToken);

                        if (gallery != null)
                        {
                            var emailNotification = new GalleryNotificationDto
                            {
                                CustomerEmail = gallery.CustomerEmail,
                                EventName = gallery.EventName,
                                AccessPin = gallery.AccessPin,
                                ShareUrl = $"http://localhost:3000/view/{payload.GalleryId}"
                            };

                            _logger.LogInformation("Dispatching notification mail to customer: {Email}", gallery.CustomerEmail);
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