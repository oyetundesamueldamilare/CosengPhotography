using CosengPhotography.Shared.Dtos;
using CosengPhotography.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CosengPhotography.Services
{
    public class EmailService : IEmailService
    {
        private readonly AzureAdSettings _azureAd;
        private readonly GraphApiSettings _graphApi;
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _httpClient;

        public EmailService(
            IOptions<AzureAdSettings> azureAd,
            IOptions<GraphApiSettings> graphApi,
            ILogger<EmailService> logger,
            HttpClient httpClient)
        {
            _azureAd = azureAd.Value;
            _graphApi = graphApi.Value;
            _logger = logger;
            _httpClient = httpClient;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var app = ConfidentialClientApplicationBuilder.Create(_azureAd.ClientId)
                .WithClientSecret(_azureAd.ClientSecret)
                .WithAuthority($"{_azureAd.Instance}{_azureAd.TenantId}")
                .Build();

            var result = await app.AcquireTokenForClient(new[] { $"https://graph.microsoft.com/.default" })
                                  .ExecuteAsync();

            return result.AccessToken;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var token = await GetAccessTokenAsync();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                message = new
                {
                    subject = subject,
                    body = new { contentType = "HTML", content = body },
                    toRecipients = new[]
                    {
                        new { emailAddress = new { address = toEmail } }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_graphApi.BaseUrl}/users/{_graphApi.SenderEmail}/sendMail", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email successfully sent to {Email}", toEmail);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email: {Error}", error);
                throw new Exception($"Graph API email send failed: {error}");
            }
        }

        public async Task SendGalleryAccessEmailAsync(GalleryNotificationDto notification)
        {
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

            await SendEmailAsync(notification.CustomerEmail, subject, htmlContent);
        }
    }

    public class AzureAdSettings
    {
        public required string Instance { get; set; }  
        public required string TenantId { get; set; }
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
    }

    public class GraphApiSettings
    {
        public required string BaseUrl { get; set; }
        public required string Scopes { get; set; } 
        public required string SenderEmail { get; set; }
    }
}
