using CosengPhotography.Interfaces;
using CosengPhotography.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CosengPhotography.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GalleryController : ControllerBase
    {
        private readonly IGalleryService _galleryService;
        private readonly ILogger<GalleryController> _logger;

        public GalleryController(IGalleryService galleryService, ILogger<GalleryController> logger)
        {
            _galleryService = galleryService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Photographer")]
        public async Task<ActionResult<GalleryDto>> CreateGallery([FromBody] GalleryCreateDto dto)
        {
            try
            {
                var photographerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(photographerId)) return Unauthorized();

                var result = await _galleryService.CreateGalleryAsync(dto, photographerId);
                return StatusCode(201, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while initializing a new gallery container.");
                return StatusCode(500, new { Message = "An internal error occurred." });
            }
        }

        [HttpPost("{galleryId:guid}/upload")]
        [Authorize(Roles = "Admin, Photographer")]
        [RequestSizeLimit(524288000)] // 500MB safety ceiling
        public async Task<IActionResult> UploadPhotos(Guid galleryId, [FromForm] List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                return BadRequest(new { Message = "No photo streams were received for upload." });
            }

            try
            {
                var updatedGallery = await _galleryService.ProcessPhotosUploadAsync(galleryId, files);
                return Ok(new
                {
                    Message = $"{files.Count} photos successfully uploaded. Notification email dispatched.",
                    Gallery = updatedGallery
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing batch file upload pipeline for gallery {GalleryId}", galleryId);
                return StatusCode(500, new { Message = "An internal processing error occurred." });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin, Photographer")]
        public async Task<IActionResult> DeleteGallery(Guid id)
        {
            try
            {
                var photographerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bool isAdmin = User.IsInRole("Admin");

                if (string.IsNullOrEmpty(photographerId)) return Unauthorized();

                await _galleryService.DeleteGalleryAsync(id, photographerId, isAdmin);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Target gallery footprint not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gallery {GalleryId}", id);
                return StatusCode(500, new { Message = "An error occurred." });
            }
        }

        [HttpGet("view/{shareId:guid}/")]
        [AllowAnonymous]
        public async Task<ActionResult<GalleryDto>> GetPublicGallery(Guid shareId)
        {
            var gallery = await _galleryService.GetGalleryByLinkAsync(shareId);
            if (gallery == null) return NotFound(new { Message = "Gallery not found." });
            return Ok(gallery);
        }


        [HttpGet("download/{photoId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPhotoDownloadLink(int photoId)
        {
            try
            {
                // 1. Fetch the direct stream payload and filename from your repository layer
                var (fileStream, fileName) = await _galleryService.GetPhotoStreamAsync(photoId);

                // 2. Detect MIME type based on file extension
                var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(fileName, out var contentType))
                {
                    contentType = "application/octet-stream"; // fallback if unknown
                }

                // 3. Return the file stream with proper headers
                return File(fileStream, contentType, fileName);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Photo metadata record missing: {Message}", ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing direct cloud stream for photo item {PhotoId}", photoId);
                return StatusCode(500, new { Message = "An internal error occurred while streaming the file download." });
            }
        }



        [HttpGet]
        [Authorize(Roles = "Admin, Photographer")]
        public async Task<ActionResult<List<GalleryDto>>> GetAllGalleries()
        {
            try
            {
                var photographerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bool isAdmin = User.IsInRole("Admin");

                if (string.IsNullOrEmpty(photographerId)) return Unauthorized();

                var galleries = await _galleryService.GetAllGalleriesAsync(photographerId, isAdmin);
                return Ok(galleries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching gallery collection data lists.");
                return StatusCode(500, new { Message = "An internal error occurred." });
            }
        }
    }
}