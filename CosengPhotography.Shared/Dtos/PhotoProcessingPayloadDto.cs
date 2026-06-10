

namespace CosengPhotography.Shared.Dtos
{
    public class PhotoProcessingPayloadDto
    {
        public Guid GalleryId { get; set; }
        public List<UploadedPhotoMetadataDto> Photos { get; set; } = new();
    }
}
