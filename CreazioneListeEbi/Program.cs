using CreazioneListe.Interfaces;
using CreazioneListe.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddMemoryCache();

// Costruisci l'applicazione
var app = builder.Build();

// Configura il middleware per gestire il pipeline delle richieste HTTP
if (!app.Environment.IsDevelopment())
{
    // Gestione degli errori in modo da mostrare una pagina amichevole agli utenti
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Abilita la visualizzazione dei dettagli degli errori per un debug più semplice
    app.UseDeveloperExceptionPage();
}

// Forza l'utilizzo di HTTPS per tutte le richieste
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Abilita l'autenticazione e l'autorizzazione (aggiungere configurazioni se necessario)
app.UseAuthentication();
app.UseAuthorization();

// Configura il percorso di default del routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
