using CosengPhotography.Shared.Dtos;



    namespace CosengPhotography.Interfaces
    {
        public interface IEmailService
        {
            Task SendGalleryAccessEmailAsync(GalleryNotificationDto notification);
        }
    }

