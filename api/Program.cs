using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Colegio.Api.Datos;
using Colegio.Api.Notas;
using Colegio.Api.Seguridad;
using Colegio.Api.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ColegioDbContext>(opciones =>
    opciones.UseNpgsql(builder.Configuration.GetConnectionString("Colegio")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SesionActual>();
builder.Services.AddSingleton<Contrasenas>();
builder.Services.AddSingleton<TokenDeAcceso>();
builder.Services.AddScoped<LectorDeNotas>();
builder.Services.AddScoped<ServicioDeSesion>();
builder.Services.AddScoped<ServicioDePlataforma>();
builder.Services.AddScoped<ServicioDeConfiguracion>();
builder.Services.AddScoped<ServicioAcademico>();
builder.Services.AddScoped<ServicioDeCalificaciones>();
builder.Services.AddScoped<ServicioDeBoletin>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Emisor"],
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Clave"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(opciones =>
    opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddControllers()
    .AddJsonOptions(opciones => opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(opciones =>
    opciones.AddPolicy("angular", politica =>
        politica.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler(errores =>
{
    errores.Run(async contexto =>
    {
        var error = contexto.Features.Get<IExceptionHandlerFeature>()?.Error;
        var (codigo, mensaje) = error switch
        {
            Colegio.Api.Reglas.ReglaDeNegocioException regla => (StatusCodes.Status400BadRequest, regla.Message),
            Colegio.Api.Reglas.NoEncontradoException noEncontrado => (StatusCodes.Status404NotFound, noEncontrado.Message),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
        };

        if (codigo == StatusCodes.Status500InternalServerError)
            contexto.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Errores").LogError(error, "Error no controlado");

        contexto.Response.StatusCode = codigo;
        await contexto.Response.WriteAsJsonAsync(new { mensaje });
    });
});

app.UseCors("angular");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ColegioActivoMiddleware>();
app.MapControllers();

await DatosIniciales.Cargar(app.Services);
app.Run();
