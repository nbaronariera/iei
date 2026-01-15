using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Logica;
using UI.Entidades; // Espacio de nombres para el DbContext y modelos

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DEL SERVIDOR ---
// Forzamos a que la aplicación corra específicamente en el puerto 8081
builder.WebHost.UseUrls("http://localhost:8081");

// --- INYECCIÓN DE DEPENDENCIAS (Servicios) ---
// Registramos LogicaCarga para que pueda ser usada en los controladores (Inyección de Dependencias)
builder.Services.AddScoped<LogicaCarga>();

// Configuramos los controladores y añadimos NewtonsoftJson para manejar ciclos infinitos 
// (útil cuando tienes relaciones de muchos a muchos en Entity Framework)
builder.Services.AddControllers()
    .AddNewtonsoftJson(o =>
        o.SerializerSettings.ReferenceLoopHandling =
            Newtonsoft.Json.ReferenceLoopHandling.Ignore);

// --- CONFIGURACIÓN DE SWAGGER (Documentación) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Localiza el archivo XML generado por el compilador para mostrar tus comentarios en la UI de Swagger
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

// --- PIPELINE DE PETICIONES HTTP (Middlewares) ---
// Habilitamos la interfaz visual de Swagger para probar los endpoints desde el navegador

app.UseSwagger();
app.UseSwaggerUI();

// Mapea las rutas de los controladores para que sean accesibles (ej. /api/productos)

app.MapControllers();

// Inicia la aplicación

app.Run();

