using CosengPhotography.Shared.Dtos;
using CosengPhotography.Interfaces;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using CosengPhotography.Models;

namespace CosengPhotography.Services
{
      public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// General purpose method to send any textual or HTML email.
        /// </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Defensive Guard: Ensure configuration parameters exist before setting up the connection socket context
            if (string.IsNullOrWhiteSpace(_emailSettings.SmtpServer) || string.IsNullOrWhiteSpace(_emailSettings.SenderEmail))
            {
                _logger.LogCritical("SMTP engine infrastructure values are completely missing from appsettings.json configuration pipelines.");
                throw new InvalidOperationException("The core email delivery engine is unconfigured.");
            }

            var message = new MimeMessage();

            // Set up sender and recipient identities via dynamic MimeKit mailbox mapping
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                // Connect to the secure Mailtrap or live production server endpoint
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);

                // Pass the authorized profile username string and secret password context
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                await client.SendAsync(message);
                _logger.LogInformation("Core system email successfully transmitted to destination client: {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send system email to address context: {Email}", toEmail);
                throw new Exception($"Failed to deliver email: {ex.Message}", ex);
            }
            finally
            {
                // Cleanly close out open socket pipelines 
                await client.DisconnectAsync(true);
            }
        }

        /// <summary>
        /// Specialized method that builds the custom photography HTML card layouts and passes it to SendEmailAsync.
        /// </summary>
        public async Task SendGalleryAccessEmailAsync(GalleryNotificationDto notification)
        {
            _logger.LogInformation("Processing custom gallery layout delivery matrices for client: {Email}", notification.CustomerEmail);

            string subject = $"Your Photo Gallery is Ready! - {notification.EventName}";

            string htmlContent = $@"
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

            try
            {
                // Reuses the freshly implemented core infrastructure method to do the actual transmitting
                await SendEmailAsync(notification.CustomerEmail, subject, htmlContent);
                _logger.LogInformation("Gallery access validation layout successfully passed off to mail socket handlers for {Email}", notification.CustomerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gallery service layout pipeline failed to dispatch to core routing for {Email}", notification.CustomerEmail);
                throw new InvalidOperationException("The email notification pipeline failed, but your gallery tracking remains secure.", ex);
            }
        }
    }
}