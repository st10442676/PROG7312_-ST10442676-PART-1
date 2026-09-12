using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SmartX.Client;

WebAssemblyHostBuilder builder =
    WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string apiBaseUrl =
    builder.Configuration["ApiBaseUrl"]
    ?? "https://localhost:7188/";

builder.Services.AddScoped(
    _ => new HttpClient
    {
        BaseAddress = new Uri(apiBaseUrl)
    });

await builder.Build().RunAsync();