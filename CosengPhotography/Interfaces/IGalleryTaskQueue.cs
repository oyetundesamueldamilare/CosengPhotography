using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Interfaces
{
    public interface IGalleryTaskQueue
    {
        ValueTask QueueUploadTaskAsync(PhotoProcessingPayloadDto payload);
        ValueTask<PhotoProcessingPayloadDto> DequeueTaskAsync(CancellationToken cancellationToken);
    }
}
