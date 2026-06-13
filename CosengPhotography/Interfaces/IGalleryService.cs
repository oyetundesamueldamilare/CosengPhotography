using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Interfaces
{
    public interface IGalleryService
    {
        Task<GalleryDto> CreateGalleryAsync(GalleryCreateDto dto, string photographerId);
        Task<GalleryDto> ProcessPhotosUploadAsync(Guid galleryId, List<IFormFile> files);
        Task DeleteGalleryAsync(Guid galleryId, string photographerId, bool isAdmin);
        Task<GalleryDto?> GetGalleryByLinkAsync(Guid shareId);
        Task<string> GetDownloadLinkAsync(int photoId);
        Task<List<GalleryDto>> GetAllGalleriesAsync(string photographerId, bool isAdmin);

        Task<(Stream FileStream, string FileName)> GetPhotoStreamAsync(int photoId);
    }
}
