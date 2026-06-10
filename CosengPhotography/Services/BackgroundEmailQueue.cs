using System.Threading.Channels;
using CosengPhotography.Interfaces;
using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Services
{
  
    public class BackgroundEmailQueue : IBackgroundEmailQueue
    {
        private readonly Channel<GalleryNotificationDto> _queue;

        public BackgroundEmailQueue()
        {
            // Bounded channel limits memory consumption if thousands of requests hit at once
            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<GalleryNotificationDto>(options);
        }

        public async ValueTask QueueEmailAsync(GalleryNotificationDto notification)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            await _queue.Writer.WriteAsync(notification);
        }

        public async ValueTask<GalleryNotificationDto> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}