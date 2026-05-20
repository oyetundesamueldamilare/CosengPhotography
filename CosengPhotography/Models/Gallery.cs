using System.ComponentModel.DataAnnotations;

namespace CosengPhotography.Models
{
    public class Gallery
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(200)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; } = string.Empty;

        [StringLength(4)] // Keeping it short for PINs
        public string AccessPin { get; set; } = string.Empty;

        public bool CanDownload { get; set; } = true;

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PublishedAt { get; set; }

        // Security: Link the gallery to the Admin/Photographer who created it
        [Required]
        public string PhotographerId { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();
    }
}