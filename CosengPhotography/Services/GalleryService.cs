using CosengPhotography.Interfaces;
using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Services
{
    public class GalleryService : IGalleryService
    {
        private readonly IGalleryRepository _galleryRepository;
        private readonly IBlobService _blobService; // Injected to decouple physical stream writes
        private readonly IGalleryTaskQueue _taskQueue; // Injected queue service
        private readonly ILogger<GalleryService> _logger;

        public GalleryService(
            IGalleryRepository galleryRepository,
            IBlobService blobService,
            IGalleryTaskQueue taskQueue,
            ILogger<GalleryService> logger)
        {
            _galleryRepository = galleryRepository;
            _blobService = blobService;
            _taskQueue = taskQueue;
            _logger = logger;
        }

        public async Task<GalleryDto> CreateGalleryAsync(GalleryCreateDto dto, string photographerId)
        {
            dto.PhotographerId = photographerId;
            return await _galleryRepository.CreateGalleryAsync(dto);
        }

        public async Task<GalleryDto> ProcessPhotosUploadAsync(Guid galleryId, List<IFormFile> files)
        {
            var payload = new PhotoProcessingPayloadDto { GalleryId = galleryId };

            // 1. Physically push files to cloud storage instantly inside the HTTP channel context
            foreach (var file in files)
            {
                using var fileStream = file.OpenReadStream();
                var fileName = Path.GetFileName(file.FileName);

                // Streams directly to Cloudflare R2 / AWS S3 / Supabase Storage Storage Bucket
                string uploadedBlobUrl = await _blobService.UploadFileAsync(fileStream, fileName);

                payload.Photos.Add(new UploadedPhotoMetadataDto
                {
                    BlobUrl = uploadedBlobUrl,
                    FileName = fileName,
                    FileSize = file.Length
                });
            }

            // 2. Drop the text-based payload map right into our Memory Channel Queue
            // This execution instruction takes less than a millisecond.
            await _taskQueue.QueueUploadTaskAsync(payload);

            // 3. Re-fetch current gallery overview data map layout to satisfy frontend expectations immediately
            var currentGalleryState = await _galleryRepository.GetGalleryByLinkAsync(galleryId);
            if (currentGalleryState == null) throw new KeyNotFoundException("Gallery instance not found.");

            return currentGalleryState;
        }

        public async Task DeleteGalleryAsync(Guid galleryId, string photographerId, bool isAdmin) =>
            await _galleryRepository.DeleteGalleryAsync(galleryId, photographerId, isAdmin);

        public async Task<GalleryDto?> GetGalleryByLinkAsync(Guid shareId) =>
            await _galleryRepository.GetGalleryByLinkAsync(shareId);

        public async Task<string> GetDownloadLinkAsync(int photoId) =>
            await _galleryRepository.GetDownloadLinkAsync(photoId);

        public async Task<List<GalleryDto>> GetAllGalleriesAsync(string photographerId, bool isAdmin) =>
            await _galleryRepository.GetAllGalleriesAsync(photographerId, isAdmin);
    }
}