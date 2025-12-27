using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using UI.Logica;
using UI.Entidades; // tu DbContext

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:8083");

builder.Services.AddScoped<LogicaParseo>();

builder.Services.AddControllers()
    .AddNewtonsoftJson(o =>
        o.SerializerSettings.ReferenceLoopHandling =
            Newtonsoft.Json.ReferenceLoopHandling.Ignore);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();