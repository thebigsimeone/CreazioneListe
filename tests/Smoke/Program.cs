using CreazioneListe.Models;
using CreazioneListe.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;

var connectionString = Environment.GetEnvironmentVariable("CREAZIONE_TEST_CONNECTION")
    ?? throw new InvalidOperationException("Impostare CREAZIONE_TEST_CONNECTION su un database LocalDB vuoto di test.");
var csb = new SqlConnectionStringBuilder(connectionString);
if (!csb.DataSource.StartsWith(@"(localdb)\CodexPublicationAudit_", StringComparison.OrdinalIgnoreCase)
    || csb.InitialCatalog != "PublicationAudit")
    throw new InvalidOperationException("Sono ammessi solo i database temporanei CodexPublicationAudit_*/PublicationAudit.");
await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
using (var setup = connection.CreateCommand())
{
    setup.CommandText = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "schema.sql"));
    await setup.ExecuteNonQueryAsync();
    setup.CommandText = @"
INSERT INTO Pratiche VALUES (2026,1,'NOR',1,'S1','CF-DEMO','VAT-DEMO','Soggetto demo','Comune demo'),
                            (2026,2,'NOR',2,'S2','CF-ESCLUSO','VAT-ESCLUSO','Soggetto escluso','Comune demo');
INSERT INTO Fornitori VALUES ('IT',1,'D','Fornitore demo');
INSERT INTO Soggetti (CodiceSoggetto,CognomeRagioneSociale,NomeSoggetto,DataNascita,ComuneNascita,NazioneNascita,TipoIndirizzo,Indirizzo,NumeroCivico,Cap,ComuneResidenza,ProvinciaResidenza,NazioneResidenza)
VALUES ('S1','Soggetto','Demo',20000101,'Comune demo','IT','VIA','Via demo','1','00000','Comune demo','XX','IT'),
       ('S2','Erede','Demo',20000102,'Comune demo','IT','VIA','Via demo','2','00000','Comune demo','XX','IT');
INSERT INTO DatoriLavoro VALUES ('S1','CON','VAT-DATORE-DEMO');
INSERT INTO Annotazioni VALUES ('S1','TEL','TEL.: 0000000001 0000000002',20260901),
                               ('S1','003','Nota dimostrativa',20260901);
INSERT INTO RelazioniSoggetti VALUES ('S1','S2','EREDE',' ');
INSERT INTO IdentificativiSoggetti VALUES ('S2','FIS','CF-EREDE-DEMO');
INSERT INTO Comuni VALUES ('Comune demo','XX');
INSERT INTO Decodifiche VALUES ('CAT01','IT','EREDE','Erede',''),('IND','IT','VIA','Via',''),('NAZ','IT','IT','Italia','IT');";
    await setup.ExecuteNonQueryAsync();
    foreach (var type in new[] { "VUT", "VSA", "BAN", "CCL", "CMO", "VED", "DP1" })
    {
        setup.CommandText = @"INSERT INTO TipiAccertamento VALUES (@type, 'Tipo demo');
INSERT INTO Assegnazioni (PraticaAnno,PraticaNumero,FornitoreNazione,FornitoreCodice,AccertamentoCodice,UrgenzaCodice,Importo,DataAffidamento,StatoAssegnazione,Esportata)
VALUES (2026,1,'IT',1,@type,'N',1,20260901,'2','N'),(2026,2,'IT',1,@type,'N',1,20260901,'2','N');";
        setup.Parameters.Clear();
        setup.Parameters.AddWithValue("@type", type);
        await setup.ExecuteNonQueryAsync();
    }
}
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
{
    ["ConnectionStrings:DefaultConnection_TENANT_A"] = connectionString,
    ["ConnectionStrings:DefaultConnection_TENANT_B"] = connectionString,
    ["QueryFilters:ExcludedClientCodes:0"] = "2",
    ["QueryFilters:MandanteSupplierCode"] = "1",
    ["Paths:TENANT_A"] = Path.Combine(Path.GetTempPath(), "PublicationAuditExports", Guid.NewGuid().ToString("N"))
}).Build();
var db = new DatabaseService(config, NullLogger<DatabaseService>.Instance);
var modulo = new ModuloService(config, NullLogger<ModuloService>.Instance);
var filter = new ColonneFiltraggioService(modulo, config);
var form = new FormData { DaDataAff="20260901", DataAff="20260930", NazCor="IT", CodCor="1" };
var count = 0;
void Check(bool condition, string label) { if (!condition) throw new Exception(label); count++; Console.WriteLine("PASS " + label); }
var summary = await db.GetSelectAsync(form,"TENANT_A");
Check(summary.Rows.Count == 7 && summary.Rows.Cast<DataRow>().All(r=>Convert.ToInt32(r["TotAcc"])==1), "summary excludes configured clients");
foreach (var type in new[] { "VUT", "VSA", "BAN", "CCL", "CMO", "VED", "DP1" })
{
    form.CodAcc=type;
    var request = new RichiestaExcel { CodAcc=type,CodUrg="N",CodCor="1",NazCor="IT",Formato="CLIENTE",TotRic=1 };
    var selected = await db.GetSelectedAsync(request,form,"TENANT_A");
    Check(selected.Rows.Count == 1, type+" detail excludes configured clients");
    var output = filter.FiltraColonne(selected,request,"TENANT_A");
    Check(output.Rows[0]["Protocollo"].ToString()=="20261" && output.Rows[0]["Codice Fiscale"].ToString()=="CF-DEMO"
        && output.Rows[0]["Mandante"].ToString()=="TENANT_A",type+" export contract");
    if(type=="VUT") Check(output.Rows[0]["TEL 1"].ToString()=="0000000001", "phone notes");
    if(type=="VSA") Check(output.Rows[0]["Partita iva"].ToString()=="VAT-DEMO", "VAT export");
}
form.CodAcc="VSA";
var heirsRequest = new RichiestaExcel { CodAcc="VSA",CodUrg="N",CodCor="1",NazCor="IT",Formato="EREDI",TotRic=1 };
var heirs = await db.GetSelectedAsync(heirsRequest,form,"TENANT_B");
var heirsOutput = filter.FiltraColonne(heirs,heirsRequest,"TENANT_B");
Check(heirsOutput.Rows[0]["PrimoRigo"].ToString()!.Contains("CF-EREDE-DEMO"), "heir query and renamed columns");
Check(modulo.LeggiModuloTesto("S1","003","TENANT_A").Contains("Nota dimostrativa"), "notes query");
var registry = new RegistroFileService(config,NullLogger<RegistroFileService>.Instance);
registry.ScriviRegistroFile(20260929,"120000",20260930,20260901,"IT",1,"VSA","N",1,"demo","demo.xlsx","B","demo","TENANT_A");
var entries = await registry.GetRegistroFilesByDataAsync("20260929","TENANT_A");
Check(entries.Count==1 && entries[0].ID_File=="demo.xlsx", "registry insert and lookup");
modulo.AggiornaFileCorrispondenti(2026,1,"VSA","N","IT","1","demo.xlsx","TENANT_A");
Check((await db.GetSelectedAsync(heirsRequest,form,"TENANT_A")).Rows.Count==0, "export status update");
try { TenantConfiguration.GetConnectionString(config,"unknown"); throw new Exception("Tenant unexpectedly accepted"); }
catch (ArgumentException) { Check(true,"unknown tenant rejected"); }
try { await db.GetSelectAsync(new FormData { DaDataAff="invalid",DataAff="20260930" },"TENANT_A"); throw new Exception("Date unexpectedly accepted"); }
catch (ArgumentException) { Check(true,"invalid date rejected"); }
try { await db.GetSelectAsync(new FormData { DaDataAff="20260930",DataAff="20260901" },"TENANT_A"); throw new Exception("Range unexpectedly accepted"); }
catch (ArgumentException) { Check(true,"inverted date range rejected"); }
Console.WriteLine($"Completed {count} checks against isolated LocalDB.");

