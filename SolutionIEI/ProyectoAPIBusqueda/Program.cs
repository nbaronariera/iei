using UI.Logica;

var builder = WebApplication.CreateBuilder(args);

// --- Configuración del Servidor ---
// Define la URL y el puerto donde escuchará la aplicación
builder.WebHost.UseUrls("http://localhost:8080");

// --- Inyección de Dependencias ---
// Registra la clase de lógica de negocio para que pueda ser usada en los controladores
// AddScoped significa que se crea una nueva instancia por cada solicitud HTTP
builder.Services.AddScoped<LogicaBusqueda>();

// --- Configuración de Controladores y JSON ---
builder.Services.AddControllers()
    .AddNewtonsoftJson(o =>
        o.SerializerSettings.ReferenceLoopHandling =
            Newtonsoft.Json.ReferenceLoopHandling.Ignore);

// --- Configuración de Swagger (Documentación API) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));



});

var app = builder.Build();

// --- Middleware (Pipeline de ejecución) ---
// Habilita la interfaz visual de Swagger para probar los endpoints desde el navegador
app.UseSwagger();
app.UseSwaggerUI();

// Mapea las rutas de los controladores (ej. [Route("api/[controller]")])
app.MapControllers();

// Inicia la aplicación 
app.Run();