namespace CosengPhotography.Dto
{
    public class GalleryDto
    {
        public Guid Id { get; set; }

        public string EventName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string AccessPin { get; set; } = string.Empty;

        public bool CanDownload { get; set; }

        public bool IsPublished { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PublishedAt { get; set; }

        public string PhotographerId { get; set; } = string.Empty;

        // Include photos as DTOs to avoid EF navigation issues
        public List<PhotoDto> Photos { get; set; } = new List<PhotoDto>();
    }
}
