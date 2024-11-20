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
builder.Services.AddScoped<IRegistroFileService, RegistroFileService>(); // Registrazione del servizio mancante

builder.Services.AddMemoryCache();

// Costruisci l'applicazione
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            if (exception != null)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(exception, "Si è verificato un errore non gestito.");
            }

            context.Response.Redirect("/Home/Error");
        });
    });
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Errore durante la gestione della richiesta HTTP per {RequestPath}", context.Request.Path);
        throw;
    }
});

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
