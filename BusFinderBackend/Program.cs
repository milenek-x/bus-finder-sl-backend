using BusFinderBackend.Firebase;
using BusFinderBackend.Repositories;
using BusFinderBackend.Services;
using Google.Cloud.Firestore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration; // Ensure this is present for IConfiguration
using System.IO;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Builder;
using BusFinderBackend.Hubs;

var builder = WebApplication.CreateBuilder(args);

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.WithOrigins("http://localhost:3000", "http://localhost:8000")
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

// Get the credential for Firebase Admin SDK using the same logic as FirebaseInit
var firebaseSection = builder.Configuration.GetSection("Firebase");
var credentialJson = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");
GoogleCredential firebaseCredential;

if (!string.IsNullOrWhiteSpace(credentialJson))
{
    firebaseCredential = GoogleCredential.FromJson(credentialJson);
}
else
{
    var credentialPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
    if (string.IsNullOrWhiteSpace(credentialPath))
    {
        credentialPath = firebaseSection["CredentialsFilePath"];
    }
    if (string.IsNullOrWhiteSpace(credentialPath) || !File.Exists(credentialPath))
    {
        throw new InvalidOperationException("Missing Google credentials: neither JSON env var nor valid file path found.");
    }
    firebaseCredential = GoogleCredential.FromFile(credentialPath);
}

// Initialize Firebase Admin SDK using the credential
FirebaseApp.Create(new AppOptions()
{
    Credential = firebaseCredential
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient(); // Register HttpClient
builder.Services.AddSignalR(); // Register SignalR

// Initialize Firestore using FirebaseInit and register as singleton
// FirebaseInit already uses GOOGLE_APPLICATION_CREDENTIALS_JSON, so this is consistent
var firestoreDb = FirebaseInit.InitializeFirestore(builder.Configuration);
builder.Services.AddSingleton(firestoreDb);
builder.Services.AddSingleton<IConfiguration>(builder.Configuration); // This line is generally not needed if IConfiguration is already available via builder.Configuration

// Register repositories and services for dependency injection
builder.Services.AddScoped<AdminRepository>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<BusStopRepository>();
builder.Services.AddScoped<BusStopService>();
builder.Services.AddScoped<StaffRepository>();
builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<PlaceRepository>();
builder.Services.AddScoped<PlaceService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<BusRouteRepository>();
builder.Services.AddScoped<BusRouteService>();
builder.Services.AddScoped<BusRepository>();
builder.Services.AddScoped<BusService>();
builder.Services.AddScoped<BusShiftRepository>();
builder.Services.AddScoped<BusShiftService>();
builder.Services.AddScoped<PassengerRepository>();
builder.Services.AddScoped<PassengerService>();
builder.Services.AddScoped<DriveImageService>();
builder.Services.AddScoped<MapService>();

// Register SignalR services
builder.Services.AddSignalR();

// Add Swagger/OpenAPI support (optional, but common for APIs)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bus Finder Backend", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bus Finder Backend V1");
    c.RoutePrefix = string.Empty; // Set to empty string to serve the Swagger UI at the app's root
});

app.UseHttpsRedirection();

// Use CORS
app.UseCors(builder => builder
    .WithOrigins("http://localhost:3000", "http://localhost:8000")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true) // Allow any origin
    .AllowCredentials()); // Allow credentials

app.UseRouting();
app.UseAuthorization(); // Ensure this is between UseRouting and MapHub
app.MapControllers();

// Map the SignalR hub
app.MapHub<BusHub>("/busHub");

app.Run();
