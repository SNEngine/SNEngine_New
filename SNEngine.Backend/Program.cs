using Microsoft.OpenApi.Models;
using SNEngine.Backend.Services;   // ← добавь эту строку

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
        };
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SNEngine Backend API",
        Version = "v1",
        Description = "API для Novel-движка на Silk.NET C#"
    });
});

// === Регистрация сервисов ===
builder.Services.AddScoped<GameBuildService>();   // ← обязательно должна быть

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // временно отключено
app.UseAuthorization();

app.MapControllers();

app.Run();