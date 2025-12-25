using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Diagnostics;
using UI.Logica;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// FORZAR PUERTO FIJO SIN ROMPER EL SISTEMA DE MÚLTIPLES APIs
// ---------------------------------------------------------------
var puertoForzado = Environment.GetEnvironmentVariable("PUERTO_API") is { } p && int.TryParse(p, out var port)
    ? port
    : 8080;

builder.WebHost.UseUrls($"http://localhost:{puertoForzado}");
builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(puertoForzado));
builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, $"http://localhost:{puertoForzado}");

// ---------------------------------------------------------------
// 1. REGISTRO DE SERVICIOS
// ---------------------------------------------------------------
builder.Services.AddScoped<LogicaBusqueda>();

// LÍNEA CLAVE: Configurar controladores ANTES de construir app
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

// Forzar que Newtonsoft sea el serializador predeterminado (elimina System.Text.Json por completo)
builder.Services.Configure<MvcOptions>(options =>
{
    options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter>();
    options.InputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonInputFormatter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = $"API Búsqueda - Puerto {puertoForzado}", Version = "v1" });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});


var app = builder.Build();

// ---------------------------------------------------------------
// 2. MIDDLEWARE
// ---------------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", $"API Búsqueda - Puerto {puertoForzado}");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();


Debug.WriteLine($"=== DIAGNÓSTICO ===");
Debug.WriteLine($"Puerto forzado detectado: {puertoForzado}");
Debug.WriteLine($"Controladores que se van a mapear: {(puertoForzado == 8080 ? "SÍ (API Búsqueda)" : "NO")}");
Debug.WriteLine($"===================");

// ---------------------------------------------------------------
// 3. ACTIVACIÓN SELECTIVA DE CONTROLADORES SEGÚN PUERTO
// ---------------------------------------------------------------
if (puertoForzado == 8080)
{
    // API de Búsqueda
    app.MapControllers();
}
else if (puertoForzado == 8081)
{
    // FUTURO: API de Carga → descomentar cuando exista el controlador
    // app.MapControllers();
}
else if (puertoForzado == 8082)
{
    // FUTURO: Wrapper Comunidad Valenciana
    // app.MapControllers();
}
else if (puertoForzado == 8083)
{
    // FUTURO: Wrapper CAT
    // app.MapControllers();
}
else if (puertoForzado == 8084)
{
    // FUTURO: Wrapper GAL
    // app.MapControllers();
}
else
{
    app.MapGet("/", () => $"API no configurada para el puerto {puertoForzado}");
}

app.Run();

