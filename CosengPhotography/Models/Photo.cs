using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CosengPhotography.Models
{
    public class Photo
    {
        [Key]
        public int Id { get; set; }
          
        [Required]
        public Guid GalleryId { get; set; }

        [Required]
        public string BlobUrl { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; } // Storing size in bytes

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
       
        [ForeignKey("GalleryId")]
        public virtual Gallery? Gallery { get; set; }
    }
}