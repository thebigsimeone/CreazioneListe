using CreazioneListe.Interfaces;
using CreazioneListe.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configura il logging per lo sviluppo locale
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddEnvironmentVariables();

// Aggiungi servizi al container
builder.Services.AddControllersWithViews();

// Aggiungi i servizi definiti nel progetto
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IModuloService, ModuloService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IColonneFiltraggioService, ColonneFiltraggioService>();
builder.Services.AddScoped<IRegistroFileService, RegistroFileService>();

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
