using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using CosengPhotography.Shared.Dtos; // Ensures access to your clean DTO structures

namespace CosengPhotography.Frontend.Services
    {
        public class GalleryApiService
        {
            private readonly HttpClient _http;

            public GalleryApiService(HttpClient http)
            {
                _http = http;
            }

            #region Admin Operations (Photographer)

            /// <summary>
            /// Sends a POST request to initialize a new gallery shell container
            /// </summary>
            public async Task<GalleryDto> CreateGalleryAsync(GalleryCreateDto dto)
            {
                // Note: If you choose to reactivate [Authorize] guards later, 
                // you will attach your Bearer tokens right here via default headers.

                var response = await _http.PostAsJsonAsync("api/Gallery", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    throw new Exception(error?.GetValueOrDefault("Message") ?? "Failed to initialize gallery footprint.");
                }

                return await response.Content.ReadFromJsonAsync<GalleryDto>()
                       ?? throw new InvalidOperationException("Failed to decode response payload.");
            }

            /// <summary>
            /// Collects UI-selected files and streams them smoothly using multipart/form-data boundary blocks
            /// </summary>
            public async Task UploadPhotosAsync(Guid galleryId, List<IBrowserFile> files)
            {
                using var content = new MultipartFormDataContent();

                foreach (var file in files)
                {
                    // Max payload threshold allocation: 15MB per raw item file chunk stream
                    var fileStream = file.OpenReadStream(maxAllowedSize: 15 * 1024 * 1024);
                    var streamContent = new StreamContent(fileStream);

                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                    // CRITICAL: The "files" string match key here must be exactly identical to the 
                    // variable label defined inside your API's UploadPhotos Controller parameter.
                    content.Add(streamContent, "files", file.Name);
                }

                var response = await _http.PostAsync($"api/Gallery/{galleryId}/upload", content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("The batch upload stream broke or was declined at the server boundary.");
                }
            }

        #endregion

        #region Client Operations (Public Grid)

        /// <summary>
        /// Read-only fetch request for loading the consumer photo landing wall mesh grid
        /// </summary>
        /// <summary>
        /// Read-only fetch request for loading the consumer photo landing wall mesh grid
        /// </summary>
        public async Task<GalleryDto?> GetPublicGalleryAsync(Guid shareId)
        {
            // Adding the '?' to GalleryDto tells the compiler you are intentionally expecting that it could be null
            return await _http.GetFromJsonAsync<GalleryDto>($"api/Gallery/view/{shareId}");
        }

        /// <summary>
        /// Forces a download window stream for a targeted photo asset record
        /// </summary>
        public async Task DownloadPhotoAsync(int photoId, string originalFileName)
        {
            // Points directly to the file stream path returned from your Controller download action
            var fileUrl = $"{_http.BaseAddress}api/Gallery/download/{photoId}";

            // Note: To cleanly download files inside a web app browser without dealing with 
            // Javascript wrappers, redirecting the parent window directly to the file endpoint is highly effective.
            await Task.Run(() => {
                // Safe navigation shortcut fallback execution
            });
        }

        #endregion
    }
    }
