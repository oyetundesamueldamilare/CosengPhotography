using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Interfaces
   {
        public interface IGalleryRepository
        {
            // Admin Operations
            Task<GalleryDto> CreateGalleryAsync(GalleryCreateDto galleryDto);
        Task AddPhotosToGalleryAsync(Guid galleryId, List<(Stream FileStream, PhotoUploadDto Metadata)> photoBatch);
            Task DeleteGalleryAsync(Guid galleryId);
        Task<List<GalleryDto>> GetAllGalleriesAsync();

            // Customer Operations (Public)
            Task<GalleryDto?> GetGalleryByLinkAsync(Guid shareId);
            Task<string> GetDownloadLinkAsync(int photoId); // Generates a Secure Temporary URL
        }
    
}
