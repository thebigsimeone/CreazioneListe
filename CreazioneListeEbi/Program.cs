using CreazioneListe.Interfaces;
using CreazioneListe.Services;


var builder = WebApplication.CreateBuilder(args);

// Configura il logging per la produzione
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configura la connessione ai servizi, incluso il caricamento della configurazione di produzione
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                     .AddEnvironmentVariables();

// Aggiungi servizi al container
builder.Services.AddControllersWithViews();

// Aggiungi i servizi definiti nel progetto
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IModuloService, ModuloService>();
builder.Services.AddScoped<IFileService, FileService>();

// Configura CORS per consentire solo richieste da domini autorizzati in produzione
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.WithOrigins("https://dominioautorizzato.com")
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

// Configura il middleware per gestire il pipeline delle richieste HTTP
if (!app.Environment.IsDevelopment())
{
    // Gestione degli errori in modo da mostrare una pagina amichevole agli utenti
    app.UseExceptionHandler("/Home/Error");
    // Imposta HSTS per aumentare la sicurezza dell'applicazione
    app.UseHsts();
}

// Forza l'utilizzo di HTTPS per tutte le richieste
app.UseHttpsRedirection();
// Consenti l'uso di file statici come JavaScript, CSS, immagini
app.UseStaticFiles();

// Abilita CORS con la policy configurata
app.UseCors("CorsPolicy");

// Abilita la protezione delle intestazioni per prevenire attacchi comuni
app.UseXContentTypeOptions();
app.UseReferrerPolicy(opts => opts.NoReferrer());
app.UseXXssProtection(opts => opts.EnabledWithBlockMode());
app.UseXfo(opts => opts.Deny());
app.UseCsp(opts => opts
    .BlockAllMixedContent()
    .StyleSources(s => s.Self().UnsafeInline())
    .FontSources(s => s.Self())
    .FormActions(s => s.Self())
    .FrameAncestors(s => s.Self())
    .ImageSources(s => s.Self().CustomSources("data:"))
    .ScriptSources(s => s.Self().UnsafeInline())
);

// Abilita il routing delle richieste
app.UseRouting();

// Abilita l'autenticazione e l'autorizzazione (aggiungere configurazioni se necessario)
app.UseAuthentication();
app.UseAuthorization();

// Configura il percorso di default del routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
