using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using EcobeeFurnaceEta.Blazor;
using EcobeeFurnaceEta.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register services
builder.Services.AddScoped<SecureTokenStorage>();
builder.Services.AddScoped<EcobeeAuthService>();
builder.Services.AddScoped<HeatLossCalculator>();
builder.Services.AddScoped<PredictionEngine>();
builder.Services.AddScoped<RuntimeStatsCache>();
builder.Services.AddScoped<EcobeeApiClient>();

await builder.Build().RunAsync();
