using BusFinderBackend.Firebase;
using BusFinderBackend.Repositories;
using BusFinderBackend.Services;
using Google.Cloud.Firestore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration; // Ensure this is present for IConfiguration
using System.IO;

var builder = WebApplication.CreateBuilder(args);

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

// Add Swagger/OpenAPI support (optional, but common for APIs)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
