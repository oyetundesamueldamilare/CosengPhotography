namespace CosengPhotography.Shared.Dtos
{
    public class PhotoDto
    {
        public int Id { get; set; }

        // Reference to the gallery this photo belongs to
        public Guid GalleryId { get; set; }

        // Public URL for accessing the photo
        public string BlobUrl { get; set; } = string.Empty;

        // Metadata
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }  // in bytes
        public DateTime UploadedAt { get; set; }
    }
}

