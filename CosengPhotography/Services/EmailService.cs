using CosengPhotography.Shared.Dtos;
using CosengPhotography.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace CosengPhotography.Services
{
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task SendGalleryAccessEmailAsync(GalleryNotificationDto notification)
        {
            // Defensive Guard: Ensure configurations are set up to prevent unhandled runtime argument crashes
            if (string.IsNullOrWhiteSpace(_smtpSettings.Host) || string.IsNullOrWhiteSpace(_smtpSettings.FromEmail))
            {
                _logger.LogCritical("SMTP engine configurations are missing or incomplete inside appsettings.json.");
                throw new InvalidOperationException("The email engine is unconfigured, but your gallery tracking remains safe.");
            }

            try
            {
                using var message = new MailMessage();

                // Set sender identity from authenticated appsettings property
                message.From = new MailAddress(_smtpSettings.FromEmail, "Coseng Photography");
                message.To.Add(new MailAddress(notification.CustomerEmail));
                message.Subject = $"Your Photo Gallery is Ready! - {notification.EventName}";

                // REFACTOR: Removed hardcoded "no-reply" address. 
                // Omitting ReplyToList allows replies to naturally route straight to your authenticated sender email.
                message.Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eee;'>
                        <h2 style='color: #333;'>Hello, Your Photos are Ready!</h2>
                        <p>The photographer has uploaded and published the memories from <strong>{notification.EventName}</strong>.</p>
                        
                        <div style='margin: 30px 0; text-align: center;'>
                            <a href='{notification.ShareUrl}' style='background-color: #000; color: #fff; padding: 12px 25px; text-decoration: none; font-weight: bold; border-radius: 4px; display: inline-block;'>View My Gallery</a>
                        </div>

                        <p style='font-size: 16px;'>Your secure access PIN is: <strong style='font-size: 20px; color: #d9534f; letter-spacing: 2px;'>{notification.AccessPin}</strong></p>
                        
                        <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                        <p style='font-size: 12px; color: #777;'>If you have any issues accessing your files, please reply directly to this email to reach your photographer.</p>
                    </div>";

                message.IsBodyHtml = true;

                using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port);
                client.Credentials = new NetworkCredential(_smtpSettings.FromEmail, _smtpSettings.Password);
                client.EnableSsl = true;

                await client.SendMailAsync(message);
                _logger.LogInformation("Gallery access email successfully sent to {Email}", notification.CustomerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute email notification delivery for target {Email}", notification.CustomerEmail);
                throw new InvalidOperationException("The email notification pipeline failed, but your gallery remains secure.", ex);
            }
        }
    }
}