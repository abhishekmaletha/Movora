using Movora.WebAPI.Configuration;

Console.WriteLine("🚀 Starting Movora.WebAPI...");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
Console.WriteLine("📦 Registering services...");
builder.Services.AddMovoraModules(builder.Configuration);

Console.WriteLine("🏗️ Building application...");
var app = builder.Build();

// Configure the HTTP request pipeline
Console.WriteLine("⚙️ Configuring HTTP pipeline...");

if (app.Environment.IsDevelopment())
{
    Console.WriteLine("📋 Adding Swagger (Development mode)...");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
// app.UseAuthentication(); // Temporarily disabled along with Keycloak
// app.UseAuthorization();  // Temporarily disabled along with Keycloak
app.MapControllers();

Console.WriteLine("🌐 Application configured successfully!");
Console.WriteLine($"🎯 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine("🔥 Starting web server...");
Console.WriteLine("📱 Swagger should be available at: https://localhost:51818/swagger");

app.Run();
