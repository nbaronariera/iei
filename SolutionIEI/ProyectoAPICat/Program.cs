using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Logica;
using UI.Entidades; // Espacio de nombres para el DbContext y modelos

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DEL SERVIDOR ---
// Define la URL y el puerto donde correrá la aplicación (Localhost, puerto 8084)
builder.WebHost.UseUrls("http://localhost:8083");

// --- REGISTRO DE SERVICIOS (Inyección de Dependencias) ---
// Registra la clase de lógica para que pueda ser usada en los controladores
builder.Services.AddScoped<LogicaParseo>();

// Configura los controladores y añade NewtonsoftJson para manejar referencias circulares
// (Evita errores cuando un objeto A apunta a B y B apunta a A)
builder.Services.AddControllers()
    .AddNewtonsoftJson(o =>
        o.SerializerSettings.ReferenceLoopHandling =
            Newtonsoft.Json.ReferenceLoopHandling.Ignore);

// --- CONFIGURACIÓN DE SWAGGER (Documentación) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Configura Swagger para que lea los comentarios XML del código y los muestre en la UI
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

// --- MIDDLEWARE / PIPELINE DE SOLICITUDES ---
// Habilita la interfaz visual de Swagger para probar los endpoints desde el navegador 

app.UseSwagger();
app.UseSwaggerUI();

// Mapea las rutas de los controladores para que la API responda a las peticiones

app.MapControllers();

// Inicia la ejecución de la aplicación

app.Run();