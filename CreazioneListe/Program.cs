using CreazioneListe.Interfaces;
using CreazioneListe.Services;
using Serilog;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configura il logging per lo sviluppo locale
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// CreateBuilder loads JSON, development user secrets, environment variables and arguments.

// Aggiungi servizi al container
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();

// Configura Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CreazioneListe API", Version = "v1" });
});

// Aggiungi i servizi definiti nel progetto
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IModuloService, ModuloService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IColonneFiltraggioService, ColonneFiltraggioService>();
builder.Services.AddScoped<IRegistroFileService, RegistroFileService>();
builder.Services.AddSingleton<SftpService>();


builder.Services.AddMemoryCache();

// Costruisci l'applicazione
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
    // Abilita Swagger in sviluppo
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CreazioneListe API V1");
        c.RoutePrefix = "swagger"; // Accessibile da /swagger
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

try
{
    Log.Information("Avvio dell'applicazione web");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Applicazione terminata a causa di un errore imprevisto");
}
finally
{
    Log.CloseAndFlush();
}

