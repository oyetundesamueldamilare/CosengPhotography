using CosengPhotography.Interfaces;
using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Services
{
    public class EmailBackgroundWorker : BackgroundService
    {
        private readonly IBackgroundEmailQueue _emailQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailBackgroundWorker> _logger;

        public EmailBackgroundWorker(
            IBackgroundEmailQueue emailQueue,
            IServiceProvider serviceProvider,
            ILogger<EmailBackgroundWorker> logger)
        {
            _emailQueue = emailQueue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Background Processing Worker started running successfully.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // This pauses execution asynchronously until a new item enters the queue
                    var notification = await _emailQueue.DequeueAsync(stoppingToken);

                    // Open an isolated dependency injection scope to fetch the Email Service securely
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        _logger.LogInformation("Processing queued access email for: {Email}", notification.CustomerEmail);
                        await emailService.SendGalleryAccessEmailAsync(notification);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal behavior when the server is turning off
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unhandled exception occurred inside the background email execution loop.");
                }
            }
        }
    }
}