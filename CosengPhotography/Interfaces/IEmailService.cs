
    using CosengPhotography.Dto;
    using System.Threading.Tasks;

    namespace CosengPhotography.Interfaces
    {
        public interface IEmailService
        {
            Task SendGalleryAccessEmailAsync(GalleryNotificationDto notification);
        }
    }

