using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using EDO.Client;
using EDO.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Явный BackendUrl в appsettings / переменных окружения; иначе — безопасный дефолт для dev:
// страница на HTTPS не может вызывать API по HTTP (mixed content), поэтому подбираем схему по BaseAddress клиента.
var configured = builder.Configuration["BackendUrl"];
var clientScheme = new Uri(builder.HostEnvironment.BaseAddress).Scheme;
var backendUrl = string.IsNullOrWhiteSpace(configured)
    ? (string.Equals(clientScheme, "https", StringComparison.OrdinalIgnoreCase)
        ? "https://localhost:7063"
        : "http://localhost:5238")
    : configured.Trim();

// Browser blocks HTTPS -> HTTP API calls (mixed content).
// If UI runs on HTTPS, force HTTPS backend endpoint for local development.
if (string.Equals(clientScheme, "https", StringComparison.OrdinalIgnoreCase) &&
    Uri.TryCreate(backendUrl, UriKind.Absolute, out var configuredUri) &&
    string.Equals(configuredUri.Scheme, "http", StringComparison.OrdinalIgnoreCase))
{
    backendUrl = "https://localhost:7063";
}

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(backendUrl) });
builder.Services.AddMudServices();
// MudBlazor 9+: DatePicker, Timeline и др. требуют TimeProvider в DI; в WASM по умолчанию его нет. Регистрируем после AddMudServices.
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// HTTP-клиент с авторизацией для API-сервисов
builder.Services.AddScoped<AuthHttpHandler>();
builder.Services.AddHttpClient("EDO.API", client =>
    client.BaseAddress = new Uri(backendUrl))
    .AddHttpMessageHandler<AuthHttpHandler>();

builder.Services.AddScoped<IRoleService>(sp =>
    new RoleService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("EDO.API")));
builder.Services.AddScoped<IUserService>(sp =>
    new UserService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("EDO.API")));
builder.Services.AddScoped<ITmcService>(sp =>
    new TmcService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("EDO.API")));
builder.Services.AddScoped<IContractorService>(sp =>
    new ContractorService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("EDO.API")));
builder.Services.AddScoped<ITemplateService>(sp =>
    new TemplateService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("EDO.API")));
builder.Services.AddScoped<IApprovalStageService>(sp =>
    new ApprovalStageService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("EDO.API")));
builder.Services.AddScoped<ITmcRequestService>(sp =>
    new TmcRequestService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("EDO.API")));
builder.Services.AddScoped<ICategoryService>(sp =>
    new CategoryService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("EDO.API")));
builder.Services.AddScoped<IDashboardService>(sp =>
    new DashboardService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("EDO.API")));
builder.Services.AddScoped<IWorkflowChainService>(sp =>
    new WorkflowChainService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("EDO.API")));

await builder.Build().RunAsync();
