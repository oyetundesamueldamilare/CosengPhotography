using CosengPhotography.Frontend;
using CosengPhotography.Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//builder.Services.AddScoped(sp => new HttpClient
//{
//    // FIXED: Now pointing directly to your active backend listening port
//    BaseAddress = new Uri("https://localhost:7075/")
//});
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Register the service wrapper to handle your forms and batch uploads
builder.Services.AddScoped<GalleryApiService>();

await builder.Build().RunAsync();
