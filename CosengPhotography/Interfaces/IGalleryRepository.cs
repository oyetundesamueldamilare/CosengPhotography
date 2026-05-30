using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Interfaces
   {
      
        public interface IGalleryRepository
        {
            Task<GalleryDto> CreateGalleryAsync(GalleryCreateDto galleryDto);
            Task AddPhotosToGalleryAsync(Guid galleryId, List<(Stream FileStream, PhotoUploadDto Metadata)> photoBatch);
            Task DeleteGalleryAsync(Guid galleryId, string photographerId, bool isAdmin);
            Task<List<GalleryDto>> GetAllGalleriesAsync(string photographerId, bool isAdmin);
            Task<GalleryDto?> GetGalleryByLinkAsync(Guid shareId);
            Task<string> GetDownloadLinkAsync(int photoId);
        }
    
    
}
