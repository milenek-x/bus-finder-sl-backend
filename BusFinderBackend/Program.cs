using BusFinderBackend.Firebase;
using BusFinderBackend.Repositories;
using BusFinderBackend.Services;
using Google.Cloud.Firestore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

// Initialize Firebase
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile("E:/FinalYearProject/Bus-Finder-SL.json"),
});

// Add services to the container.
builder.Services.AddControllers();

// Initialize Firestore using FirebaseInit and register as singleton
var firestoreDb = FirebaseInit.InitializeFirestore(builder.Configuration);
builder.Services.AddSingleton(firestoreDb);
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);


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

