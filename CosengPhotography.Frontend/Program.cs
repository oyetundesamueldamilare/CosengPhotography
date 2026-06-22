using CosengPhotography.Frontend;
using CosengPhotography.Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// =========================================================================
// DYNAMIC BACKEND ROUTING ENGINE
// =========================================================================
string currentHostUrl = builder.HostEnvironment.BaseAddress;
string targetBackendApiUrl;

// Check if the application is executing inside a local development container/machine
if (currentHostUrl.Contains("localhost") || currentHostUrl.Contains("127.0.0.1"))
{
    // Local development backend API address
    targetBackendApiUrl = "https://localhost:7075/";
}
else
{
    // Hardcoded production backend API service URL on Render
    // Replace this string with your actual live Render Backend Web Service URL
    targetBackendApiUrl = "https://cosengphotography-api.onrender.com/";
}

builder.Services.AddScoped(sp => new HttpClient
{
    // Set the BaseAddress safely ensuring it points to the accurate API instance
    BaseAddress = new Uri(targetBackendApiUrl)
});

// Register the service wrapper to handle your forms and batch uploads
builder.Services.AddScoped<GalleryApiService>();

await builder.Build().RunAsync();