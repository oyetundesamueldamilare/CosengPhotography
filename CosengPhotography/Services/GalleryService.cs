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
        private readonly IEmailService _emailService; // Injected email service for notification handling
        private readonly IConfiguration _configuration; // For resolving dynamic configuration values

        public GalleryService(
            IGalleryRepository galleryRepository,
            IBlobService blobService,
            IGalleryTaskQueue taskQueue,
            ILogger<GalleryService> logger,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _galleryRepository = galleryRepository;
            _blobService = blobService;
            _taskQueue = taskQueue;
            _logger = logger;
            _emailService = emailService;
            _configuration = configuration;
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
                string uploadedBlobUrl = await _blobService.UploadFileAsync(fileStream, fileName, galleryId);

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

        public async Task DeleteGalleryAsync(Guid galleryId, string photographerId, bool isAdmin)
        {
            // 1. Tell the repository to delete the database records and return the file URLs
            List<string> photoUrls = await _galleryRepository.DeleteGalleryAsync(galleryId, photographerId, isAdmin);

            // 2. If the deleted gallery contained photos, drop the cleanup payload into our background queue!
            if (photoUrls != null && photoUrls.Any())
            {
                var cleanupPhotosList = photoUrls.Select(url => new UploadedPhotoMetadataDto
                {
                    BlobUrl = url
                }).ToList();

                var cleanupPayload = new PhotoProcessingPayloadDto
                {
                    GalleryId = galleryId,
                    Photos = cleanupPhotosList
                };

                // Hand off the slow cloud erasure work to the background channel instantly
                await _taskQueue.QueueUploadTaskAsync(cleanupPayload);
            }
        }

        public async Task<GalleryDto?> GetGalleryByLinkAsync(Guid shareId) =>
            await _galleryRepository.GetGalleryByLinkAsync(shareId);

        public async Task<string> GetDownloadLinkAsync(int photoId) =>
            await _galleryRepository.GetDownloadLinkAsync(photoId);

        public async Task<List<GalleryDto>> GetAllGalleriesAsync(string photographerId, bool isAdmin) =>
            await _galleryRepository.GetAllGalleriesAsync(photographerId, isAdmin);

        public async Task<(Stream FileStream, string FileName)> GetPhotoStreamAsync(int photoId) =>
            await _galleryRepository.GetPhotoStreamAsync(photoId);
        public async Task<bool> ResendGalleryNotificationAsync(Guid galleryId)
        {
            // 1. Fetch fresh data by leveraging your existing repository lookup method
            var gallery = await _galleryRepository.GetGalleryByLinkAsync(galleryId);
            if (gallery == null)
            {
                _logger.LogWarning("Resend notification aborted. Gallery metadata for {GalleryId} not found.", galleryId);
                return false;
            }

            // 2. Resolve the active production or local domain
            string baseUrl = _configuration["FrontendBaseUrl"] ?? "https://localhost:7111";
            baseUrl = baseUrl.TrimEnd('/');

            // 3. Assemble the notification payload matching your background worker pattern
            var emailNotification = new GalleryNotificationDto
            {
                CustomerEmail = gallery.CustomerEmail,
                EventName = gallery.EventName,
                AccessPin = gallery.AccessPin,
                ShareUrl = $"{baseUrl}/view/{galleryId}"
            };

            try
            {
                _logger.LogInformation("Service layer re-dispatching notification mail for gallery: {GalleryId}", galleryId);
                await _emailService.SendGalleryAccessEmailAsync(emailNotification);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete email transmission for gallery {GalleryId} in service layer.", galleryId);
                throw;
            }
        }
    }
}