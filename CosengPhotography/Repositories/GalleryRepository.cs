using CosengPhotography.Data;
using CosengPhotography.Shared.Dtos;
using CosengPhotography.Interfaces;
using CosengPhotography.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;


namespace CosengPhotography.Repositories
{
    public class GalleryRepository : IGalleryRepository
    {
        private readonly AppDbContext _context;
        private readonly IBlobService _blobService;
        private readonly ILogger<GalleryRepository> _logger;

        public GalleryRepository(AppDbContext context, IBlobService blobService, ILogger<GalleryRepository> logger)
        {
            _context = context;
            _blobService = blobService;
            _logger = logger;
        }

        #region Admin Operations (Photographer)

        public async Task<GalleryDto> CreateGalleryAsync(GalleryCreateDto galleryDto)
        {
            var gallery = new Gallery
            {
                Id = Guid.NewGuid(),
                EventName = galleryDto.EventName,
                CustomerEmail = galleryDto.CustomerEmail,
                AccessPin = GenerateRandomPin(4),
                CreatedAt = DateTime.UtcNow,
                IsPublished = false,
                CanDownload = true
            };

            _context.Galleries.Add(gallery);
            await _context.SaveChangesAsync();

            return MapToDto(gallery);
        }

        // REFACTOR: Method signature decoupled from IFormFile / Stream structures inside DTOs
        public async Task AddPhotosToGalleryAsync(Guid galleryId, List<(Stream FileStream, PhotoUploadDto Metadata)> photoBatch)
        {
            if (photoBatch == null || !photoBatch.Any()) return;

            var galleryExists = await _context.Galleries.AnyAsync(g => g.Id == galleryId);
            if (!galleryExists) throw new KeyNotFoundException("Gallery not found.");

            var photoEntities = new List<Photo>(photoBatch.Count);

            foreach (var item in photoBatch)
            {
                try
                {
                    // Stream and DTO are processed side-by-side cleanly
                    string relativeUrl = await _blobService.UploadFileAsync(item.FileStream, item.Metadata.FileName);

                    photoEntities.Add(new Photo
                    {
                        GalleryId = galleryId,
                        BlobUrl = relativeUrl,
                        FileName = item.Metadata.FileName,
                        FileSize = item.Metadata.FileSize,
                        UploadedAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed physical upload loop for file {FileName} in gallery {GalleryId}.", item.Metadata.FileName, galleryId);
                }
                finally
                {
                    // CRITICAL: Ensure we dispose of the unmanaged file stream right here 
                    // where the operation concludes, eliminating potential server file locks.
                    await item.FileStream.DisposeAsync();
                }
            }

            if (photoEntities.Any())
            {
                await _context.Photos.AddRangeAsync(photoEntities);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteGalleryAsync(Guid galleryId)
        {
            var photoUrls = await _context.Photos
                .Where(p => p.GalleryId == galleryId)
                .Select(p => p.BlobUrl)
                .ToListAsync();

            var deleteTasks = photoUrls.Select(url => _blobService.DeleteFileAsync(url));
            await Task.WhenAll(deleteTasks);

            var gallery = await _context.Galleries.FirstOrDefaultAsync(g => g.Id == galleryId);
            if (gallery == null) throw new KeyNotFoundException();

            _context.Galleries.Remove(gallery);
            await _context.SaveChangesAsync();
        }
        public async Task<List<GalleryDto>> GetAllGalleriesAsync()
        {
            // Query database contexts efficiently without eagerly dragging unneeded physical payloads
            return await _context.Galleries
                .AsNoTracking() // Optimizes performance for read-only tracking profiles
                .OrderByDescending(g => g.CreatedAt) // Keeps your newest photo shoots right at the top
                .Select(g => new GalleryDto
                {
                    Id = g.Id,
                    EventName = g.EventName,
                    CustomerEmail = g.CustomerEmail,
                    AccessPin = g.AccessPin,
                    CanDownload = g.CanDownload,
                    CreatedAt = g.CreatedAt,
                    Photos = g.Photos.Select(p => new PhotoDto
                    {
                        Id = p.Id,
                        BlobUrl = p.BlobUrl,
                        FileName = p.FileName
                    }).ToList()
             
        })
                .ToListAsync();
        }

        #endregion

        #region Customer Operations (Public)

        public async Task<GalleryDto?> GetGalleryByLinkAsync(Guid shareId)
        {
            var gallery = await _context.Galleries
                .AsNoTracking()
                .Include(g => g.Photos)
                .FirstOrDefaultAsync(g => g.Id == shareId);

            if (gallery == null) return null;

            return MapToDto(gallery);
        }

        public async Task<string> GetDownloadLinkAsync(int photoId)
        {
            var photo = await _context.Photos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == photoId);

            if (photo == null) throw new KeyNotFoundException();

            return await _blobService.GetSecureUrlAsync(photo.BlobUrl);
        }

        #endregion

        #region Private Helpers

        private string GenerateRandomPin(int length)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return string.Concat(bytes.Select(b => (b % 10).ToString()));
        }

        private static GalleryDto MapToDto(Gallery gallery)
        {
            return new GalleryDto
            {
                Id = gallery.Id,
                EventName = gallery.EventName,
                CustomerEmail = gallery.CustomerEmail,
                AccessPin = gallery.AccessPin,
                CanDownload = gallery.CanDownload,
                CreatedAt = gallery.CreatedAt,
                Photos = gallery.Photos.Select(p => new PhotoDto
                {
                    Id = p.Id,
                    BlobUrl = p.BlobUrl,
                    FileName = p.FileName
                }).ToList()
            };
        }

        #endregion
    }
}