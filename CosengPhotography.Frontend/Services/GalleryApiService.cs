using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CosengPhotography.Shared.Dtos;

namespace CosengPhotography.Frontend.Services
{
    public class GalleryApiService
    {
        private readonly HttpClient _http;

        // Caches the token in memory for the duration of the browser session
        private string? _jwtToken;

        public GalleryApiService(HttpClient http)
        {
            _http = http;
        }

        // =========================================================================
        // AUTHENTICATION PIPELINE HANDSHAKES
        // =========================================================================
        public async Task<FrontendAuthResult> LoginAsync(LoginDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", dto);
            var result = await HandleAuthResponseAsync(response);

            if (result.IsSuccess)
            {
                // Capture the token securely upon successful authentication
                _jwtToken = result.Token;
            }

            return result;
        }

        public async Task<FrontendAuthResult> RegisterAsync(RegisterDto dto)
        {
            // Now sending the full payload including the selected role string directly to the BE
            var response = await _http.PostAsJsonAsync("api/auth/register", dto);

            if (response.IsSuccessStatusCode)
            {
                return new FrontendAuthResult { IsSuccess = true };
            }

            // Safely reads raw text blocks or validation failures from the backend
            var error = await response.Content.ReadAsStringAsync();
            return new FrontendAuthResult
            {
                IsSuccess = false,
                ErrorMessage = string.IsNullOrWhiteSpace(error) ? $"Server Error Code: {(int)response.StatusCode}" : error
            };
        }

        // =========================================================================
        // DATA HISTORY & SPACE PROVISIONING
        // =========================================================================
        public async Task<List<GalleryDto>> GetAllGalleriesAsync()
        {
            // Apply authorization headers right before firing the request
            AttachAuthorizationHeader();

            var response = await _http.GetAsync("api/gallery");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<GalleryDto>>()
                       ?? new List<GalleryDto>();
            }

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"[Route Error] Server responded with code {(int)response.StatusCode}: {error}");
        }

        public async Task<GalleryDto> CreateGalleryAsync(GalleryCreateDto dto)
        {
            AttachAuthorizationHeader();

            var response = await _http.PostAsJsonAsync("api/gallery", dto);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<GalleryDto>()
                       ?? throw new Exception("Empty entity body received.");
            }

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Creation Failed: {error}");
        }

        public async Task DeleteGalleryAsync(Guid id)
        {
            AttachAuthorizationHeader();

            var response = await _http.DeleteAsync($"api/gallery/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Purge Failed: {error}");
            }
        }

        public async Task UploadPhotosAsync(Guid galleryId, List<Microsoft.AspNetCore.Components.Forms.IBrowserFile> files)
        {
            AttachAuthorizationHeader();

            using var content = new MultipartFormDataContent();
            foreach (var file in files)
            {
                var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 15));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "files", file.Name);
            }

            var response = await _http.PostAsync($"api/gallery/{galleryId}/upload", content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Stream upload processing fault: {error}");
            }
        }

        // =========================================================================
        // NEW FEATURE: MANUAL NOTIFICATION RESEND PIPELINE
        // =========================================================================
        /// <summary>
        /// Requests the backend to instantly regenerate and re-dispatch the access email for a specific gallery.
        /// </summary>
        public async Task<bool> ResendGalleryEmailAsync(Guid galleryId)
        {
            // Ensures the request maps your photographer's active authentication token identity
            AttachAuthorizationHeader();

            // Matches route: POST api/gallery/{id}/resend-notification
            var response = await _http.PostAsync($"api/gallery/{galleryId}/resend-notification", null);

            return response.IsSuccessStatusCode;
        }

        // =========================================================================
        // PUBLIC CONTENT RETRIEVAL PIPELINES
        // =========================================================================
        public async Task<GalleryDto> GetGalleryByIdAsync(Guid shareId)
        {
            // Public endpoint doesn't require token context mapping
            var response = await _http.GetAsync($"api/gallery/view/{shareId}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<GalleryDto>()
                       ?? throw new Exception("Failed to deserialize gallery payload.");
            }

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"[Gallery Retrieval Error] Server responded with code {(int)response.StatusCode}: {error}");
        }

        public async Task<Stream> DownloadPhotoAsync(Guid photoId)
        {
            // Calls your backend controller endpoint
            var response = await _http.GetAsync($"api/gallery/download/{photoId}");

            if (response.IsSuccessStatusCode)
            {
                // Returns the raw file binary stream directly to your Blazor component
                return await response.Content.ReadAsStreamAsync();
            }

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Download failed. Server responded with: {error}");
        }

        // =========================================================================
        // PRIVATE UTILITY LIFECYCLES
        // =========================================================================
        private void AttachAuthorizationHeader()
        {
            if (!string.IsNullOrEmpty(_jwtToken))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
            }
        }

        private async Task<FrontendAuthResult> HandleAuthResponseAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                return new FrontendAuthResult { IsSuccess = true, Token = result?.Token ?? string.Empty };
            }

            var error = await response.Content.ReadAsStringAsync();
            return new FrontendAuthResult
            {
                IsSuccess = false,
                ErrorMessage = string.IsNullOrWhiteSpace(error) ? $"Server code: {(int)response.StatusCode}" : error
            };
        }

        private class LoginResponse { public string Token { get; set; } = string.Empty; }
    }
}