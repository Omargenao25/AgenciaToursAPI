using TourSmart.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TourSmart.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS obligatorio en producción
        options.LoginPath = "/cuenta/acceder";
        options.AccessDeniedPath = "/cuenta/denegado";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = async context =>
        {
            var services = context.HttpContext.RequestServices;
            var cache = services.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var sessionId = context.Principal?.FindFirst("SessionId")?.Value;
            if (string.IsNullOrWhiteSpace(sessionId) ||
                !cache.TryGetValue("jwt:" + sessionId, out object? value) ||
                value is not string jwt || string.IsNullOrWhiteSpace(jwt))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            // El cambio de contraseña también invalida cookies antiguas, no solo
            // las llamadas directas a la API. Validación fail-closed para viajeros.
            if (context.Principal?.IsInRole("Cliente") == true)
            {
                try
                {
                    var client = services.GetRequiredService<IHttpClientFactory>()
                        .CreateClient("TourSmart.Sesion");
                    using var request = new HttpRequestMessage(HttpMethod.Get, "api/MiCuenta/sesion");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                    using var response = await client.SendAsync(request, context.HttpContext.RequestAborted);
                    if (response.IsSuccessStatusCode) return;
                }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) when (!context.HttpContext.RequestAborted.IsCancellationRequested) { }

                cache.Remove("jwt:" + sessionId);
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpClient<IToursApiClient, ToursApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5290/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5290/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddHttpClient<IReservasAdminClient, ReservasAdminClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5290/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddHttpClient<IToursAdminClient, ToursAdminClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5290/");
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddHttpClient<IClientesAdminClient, ClientesAdminClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5290/");
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddHttpClient<IMiCuentaApiClient, MiCuentaApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5290/");
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddHttpClient("TourSmart.Sesion", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5290/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
