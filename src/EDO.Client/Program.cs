using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using EDO.Client;
using EDO.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();

builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// HTTP-клиент с авторизацией для API-сервисов
builder.Services.AddScoped<AuthHttpHandler>();
builder.Services.AddHttpClient("EDO.API", client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<AuthHttpHandler>();

builder.Services.AddScoped<IRoleService>(sp =>
    new RoleService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("EDO.API")));
builder.Services.AddScoped<IUserService>(sp =>
    new UserService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("EDO.API")));

await builder.Build().RunAsync();
