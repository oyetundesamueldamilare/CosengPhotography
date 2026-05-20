using CosengPhotography.Dto;
using CosengPhotography.Interfaces;
using System.Net;
using System.Net.Mail;

namespace CosengPhotography.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendGalleryAccessEmailAsync(GalleryNotificationDto notification)
        {
            try
            {
                // Read configuration settings
                var smtpHost = _config["SmtpSettings:Host"];
                var smtpPort = int.Parse(_config["SmtpSettings:Port"] ?? "587");
                var fromEmail = _config["SmtpSettings:FromEmail"];
                var password = _config["SmtpSettings:Password"];

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail ?? "no-reply@cosengphotography.com", "Coseng Photography");
                message.To.Add(new MailAddress(notification.CustomerEmail));
                message.Subject = $"Your Photo Gallery is Ready! - {notification.EventName}";

                // Build a clean HTML body for the client
                message.Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee;'>
                        <h2 style='color: #333;'>Hello, Your Photos are Ready!</h2>
                        <p>The photographer has uploaded and published the memories from <strong>{notification.EventName}</strong>.</p>
                        
                        <div style='margin: 30px 0; text-align: center;'>
                            <a href='{notification.ShareUrl}' style='background-color: #000; color: #fff; padding: 12px 25px; text-decoration: none; font-weight: bold; border-radius: 4px;'>View My Gallery</a>
                        </div>

                        <p style='font-size: 16px;'>Your secure access PIN is: <strong style='font-size: 20px; color: #d9534f; letter-spacing: 2px;'>{notification.AccessPin}</strong></p>
                        
                        <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                        <p style='font-size: 12px; color: #777;'>If you have any issues accessing your files, please reply directly to your photographer.</p>
                    </div>";

                message.IsBodyHtml = true;

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.Credentials = new NetworkCredential(fromEmail, password);
                client.EnableSsl = true;

                await client.SendMailAsync(message);
                _logger.LogInformation("Gallery access email successfully sent to {Email}", notification.CustomerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send gallery access email to {Email}", notification.CustomerEmail);
                // We catch internally so a failed email server doesn't break the database transaction
                throw new InvalidOperationException("The email notification pipeline failed, but your gallery remains secure.", ex);
            }
        }
    }
}