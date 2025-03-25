using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Reflection;
using Movora.Core.Core.Hosting;
using Movora.WebAPI;

var builder = WebApplication.CreateBuilder(args);

// Register IConfiguration to make it available throughout the application
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var searchPattern = "Movora"; // Search pattern for assemblies to load
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
MediatorModulePackage.Bootstrap(builder.Services, searchPattern); // Register MediatR services
builder.Services.AddControllers(); // Register controllers

// Set the current directory to the base path of the application
Directory.SetCurrentDirectory(builder.Environment.ContentRootPath);

ModulePackage.RegisterServices(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Movora.WebAPI v1");
        c.RoutePrefix = string.Empty; // Access Swagger at the root URL
    });
}

app.UseHttpsRedirection();

app.UseRouting(); // Enable routing

app.UseAuthorization(); // Enable authorization middleware

app.MapControllers(); // Map all controllers

app.Run();

