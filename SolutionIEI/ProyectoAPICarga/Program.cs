using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Logica;
using UI.Entidades; // tu DbContext

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:8081");

builder.Services.AddScoped<LogicaCarga>();

builder.Services.AddControllers()
    .AddNewtonsoftJson(o =>
        o.SerializerSettings.ReferenceLoopHandling =
            Newtonsoft.Json.ReferenceLoopHandling.Ignore);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

