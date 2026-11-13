using AgenciaToursAPI.Data;
using AgenciaToursAPI.Models;
using AgenciaToursAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Conexion")
    )
);


builder.Services.AddScoped<
    IPasswordHasher<Usuario>,
    PasswordHasher<Usuario>
>();


var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("La clave JWT no está configurada.");

// Evitar arrancar producción con una clave débil o un valor de ejemplo.
// Desarrollo conserva la configuración de User Secrets que ya usa TourSmart.
if (!builder.Environment.IsDevelopment() &&
    (Encoding.UTF8.GetByteCount(jwtKey) < 32 ||
     jwtKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
     jwtKey.Contains("example", StringComparison.OrdinalIgnoreCase)))
    throw new InvalidOperationException(
        "Configura una clave JWT de al menos 32 bytes y sin valores de ejemplo antes del despliegue.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("El emisor JWT no está configurado.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("La audiencia JWT no está configurada.");


builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                ),

             
                ClockSkew = TimeSpan.Zero
            };

 
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var tokenVersion = context.Principal?.FindFirst("version_sesion")?.Value;
                if (!int.TryParse(userIdText, out var userId) || string.IsNullOrWhiteSpace(tokenVersion))
                {
                    context.Fail("La sesión no tiene datos de validación.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var versionActual = await db.Usuarios.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => u.VersionSesion)
                    .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

                if (!string.Equals(versionActual, tokenVersion, StringComparison.Ordinal))
                    context.Fail("La sesión fue revocada o el usuario ya no existe.");
            },

            OnAuthenticationFailed = context =>
            {
                // Solo registramos el tipo de error. Nunca registrar el token JWT.
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("TourSmart.Jwt");

                logger.LogWarning(
                    "JWT rechazado en {Ruta}. Tipo de error: {TipoError}",
                    context.Request.Path,
                    context.Exception.GetType().Name);

                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                
                context.HandleResponse();

                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                context.Response.ContentType =
                    "application/json; charset=utf-8";

                string mensaje;

                if (context.AuthenticateFailure
                    is SecurityTokenExpiredException)
                {
                    mensaje = "El token ha expirado. Debe iniciar sesión nuevamente.";
                }
                else if (context.AuthenticateFailure != null)
                {
                    mensaje = "El token proporcionado no es válido.";
                }
                else
                {
                    mensaje = "Debe iniciar sesión para acceder a este endpoint.";
                }

                var respuesta = JsonSerializer.Serialize(new
                {
                    mensaje
                });

                await context.Response.WriteAsync(respuesta);
            },

            OnForbidden = async context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status403Forbidden;

                context.Response.ContentType =
                    "application/json; charset=utf-8";

                var respuesta = JsonSerializer.Serialize(new
                {
                    mensaje = "No tiene permiso para realizar esta operación."
                });

                await context.Response.WriteAsync(respuesta);
            }
        };
    });


builder.Services.AddAuthorization();


builder.Services.AddScoped<CorreoRecuperacionService>();
builder.Services.AddScoped<NotificacionesReservaService>();
builder.Services.AddScoped<VerificacionCorreoService>();
builder.Services.AddHostedService<VencimientoReservasService>();
builder.Services.AddControllers();


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Agencia Tours API",
            Version = "v1"
        }
    );

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Introduzca el token JWT obtenido al iniciar sesión."
        }
    );

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        }
    );
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(handler =>
    {
        handler.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                mensaje = "Ha ocurrido un error interno. Intenta nuevamente."
            });
        });
    });
    app.UseHsts();
}

// Endurecimiento de respuestas; no cambia la semántica de los endpoints.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();