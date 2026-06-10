using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Interfaces
{
    public interface IBackgroundEmailQueue
    {
        ValueTask QueueEmailAsync(GalleryNotificationDto notification);
        ValueTask<GalleryNotificationDto> DequeueAsync(CancellationToken cancellationToken);
    }
}
