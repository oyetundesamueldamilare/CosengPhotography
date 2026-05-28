using CosengPhotography.Shared.Dtos;
using CosengPhotography.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace CosengPhotography.Controllers
{
    [ApiController]                                                                     
    [Route("api/[controller]")]
    public class GalleryController : ControllerBase
    {
        private readonly IGalleryRepository _galleryRepository;
        private readonly ILogger<GalleryController> _logger;
        private readonly IEmailService _emailService;

        public GalleryController(IGalleryRepository galleryRepository, ILogger<GalleryController> logger, IEmailService emailService)
        {
            _galleryRepository = galleryRepository;
            _logger = logger;
            _emailService = emailService;
        }

        #region Admin Operations 

        /// <summary>
        /// Creates an empty structural gallery container, automatically generating the shareable ID and secure PIN.
        /// </summary>
        [HttpPost]              
        [Authorize(Roles = "Admin, Photographer")] // Uncomment when Identity JWT configurations are fully finalized
        public async Task<ActionResult<GalleryDto>> CreateGallery([FromBody] GalleryCreateDto dto)
        {
            try
            {
                var result = await _galleryRepository.CreateGalleryAsync(dto);
                return StatusCode(201, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while initializing a new gallery container.");
                return StatusCode(500, new { Message = "An internal error occurred while setting up the gallery footprint." });
            }
        }

        /// <summary>
        /// Accepts a multipart/form-data request containing an array of high-res files and uploads them.
        /// </summary>
        [HttpPost("{galleryId:guid}/upload")]
        // [Authorize] 
        [RequestSizeLimit(524288000)] // 500MB safety ceiling for batch uploads
        public async Task<IActionResult> UploadPhotos(Guid galleryId, [FromForm] List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                return BadRequest(new { Message = "No photo streams were received for upload." });
            }

            List<(Stream FileStream, PhotoUploadDto Metadata)>? photoBatch = null;

            try
            {
                // Materialize the incoming network streams
                photoBatch = files.Select(file => (
                    FileStream: file.OpenReadStream(),
                    Metadata: new PhotoUploadDto
                    {
                        FileName = Path.GetFileName(file.FileName),
                        FileSize = file.Length
                    }
                )).ToList();

                // 1. Process physical file storage and write database records
                await _galleryRepository.AddPhotosToGalleryAsync(galleryId, photoBatch);

                // 2. Fetch the newly populated gallery data to build the notification
                var galleryData = await _galleryRepository.GetGalleryByLinkAsync(galleryId);

                if (galleryData != null)
                {
                    // Build the shareable frontend URL dynamically
                    // Note: Update "localhost:3000" or replace it via builder.Configuration["App:FrontendUrl"] in production
                    string frontendViewUrl = $"http://localhost:3000/view/{galleryId}";

                    var emailNotification = new GalleryNotificationDto
                    {
                        CustomerEmail = galleryData.CustomerEmail,
                        EventName = galleryData.EventName,
                        AccessPin = galleryData.AccessPin,
                        ShareUrl = frontendViewUrl
                    };

                    // 3. Fire-and-forget: Dispatch the email asynchronously on a background worker thread
                    // This prevents your frontend app from freezing while waiting for SMTP network responses
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendGalleryAccessEmailAsync(emailNotification);
                        }
                        catch (Exception mailEx)
                        {
                            _logger.LogError(mailEx, "Background notification dispatch failed for gallery {GalleryId}", galleryId);
                        }
                    });
                }

                return Ok(new { Message = $"{files.Count} photos successfully uploaded and added to the gallery. Access email dispatched." });
            }
            catch (KeyNotFoundException ex)
            {
                await SafeCleanupStreamsAsync(photoBatch);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                await SafeCleanupStreamsAsync(photoBatch);
                _logger.LogError(ex, "Error executing batch file upload pipeline for gallery {GalleryId}", galleryId);
                return StatusCode(500, new { Message = "An internal processing error occurred while writing files to disk." });
            }
        }

        /// <summary>
        /// Deletes a gallery entry along with its physical file system traces.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteGallery(Guid id)
        {
            try
            {
                await _galleryRepository.DeleteGalleryAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Target gallery footprint not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gallery {GalleryId}", id);
                return StatusCode(500, new { Message = "An error occurred while deleting the gallery." });
            }
        }

        #endregion

        #region Customer Operations (Public)

        /// <summary>
        /// Read-only endpoint for the client-side landing grid using the unindexed shareable GUID.
        /// </summary>
        [HttpGet("view/{shareId:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<GalleryDto>> GetPublicGallery(Guid shareId)
        {
            var gallery = await _galleryRepository.GetGalleryByLinkAsync(shareId);

            if (gallery == null)
            {
                return NotFound(new { Message = "This gallery is no longer active or could not be found." });
            }

            return Ok(gallery);
        }

        /// <summary>
        /// Returns the secure relative file path for an item download request.
        /// </summary>
        [HttpGet("download/{photoId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPhotoDownloadLink(int photoId)
        {
            try
            {
                var downloadUrl = await _galleryRepository.GetDownloadLinkAsync(photoId);
                return Ok(new { Url = downloadUrl });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "The requested photo could not be located." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating download path for photo item {PhotoId}", photoId);
                return StatusCode(500, new { Message = "Could not retrieve file download link." });
            }
        }
        /// <summary>
        /// Retrieves all gallery containers in the database to populate the dashboard log history.
        /// </summary>
        [HttpGet]
        [Authorize] // Matches your creation authorization scheme
        public async Task<ActionResult<List<GalleryDto>>> GetAllGalleries()
        {
            try
            {
                // Assuming your repository interface has a method to retrieve all galleries
                var galleries = await _galleryRepository.GetAllGalleriesAsync();
                return Ok(galleries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching gallery collection data lists.");
                return StatusCode(500, new { Message = "An internal error occurred while synchronizing database log blocks." });
            }
        }

        #endregion

        #region Private Fallback Helpers

        private static async Task SafeCleanupStreamsAsync(List<(Stream FileStream, PhotoUploadDto Metadata)>? batch)
        {
            if (batch == null) return;

            foreach (var item in batch)
            {
                if (item.FileStream != null)
                {
                    try
                    {
                        await item.FileStream.DisposeAsync();
                    }
                    catch
                    {
                        // Passive suppression: prevent error-handling crashes during pipeline failure states
                    }
                }
            }
        }

        #endregion
    }
}