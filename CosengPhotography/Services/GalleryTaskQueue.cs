using System.Threading.Channels;
using CosengPhotography.Interfaces;
using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Services
{
      public class GalleryTaskQueue : IGalleryTaskQueue
    {
        private readonly Channel<PhotoProcessingPayloadDto> _queue;

        public GalleryTaskQueue()
        {
            // Bounded channel keeps your application's RAM usage capped safely
            var options = new BoundedChannelOptions(2000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<PhotoProcessingPayloadDto>(options);
        }

        public async ValueTask QueueUploadTaskAsync(PhotoProcessingPayloadDto payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            await _queue.Writer.WriteAsync(payload);
        }

        public async ValueTask<PhotoProcessingPayloadDto> DequeueTaskAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}