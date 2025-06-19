using BusFinderBackend.Firebase;
using BusFinderBackend.Repositories;
using BusFinderBackend.Services;
using Google.Cloud.Firestore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration; // Ensure this is present for IConfiguration

var builder = WebApplication.CreateBuilder(args);

// Get the credential JSON from the environment variable
var credentialJson = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");

if (string.IsNullOrWhiteSpace(credentialJson))
{
    throw new InvalidOperationException("Missing Google credentials JSON in environment variable 'GOOGLE_APPLICATION_CREDENTIALS_JSON'.");
}

// Initialize Firebase Admin SDK using the credential JSON
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromJson(credentialJson)
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
