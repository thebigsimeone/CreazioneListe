using CreazioneListe.Models;
using CreazioneListe.Services;
using CreazioneListe.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Reflection;

var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
{
    ["ConnectionStrings:DefaultConnection_TENANT_A"]="Server=(localdb)\\Offline;Database=DemoA;Integrated Security=true",
    ["ConnectionStrings:DefaultConnection_TENANT_B"]="Server=(localdb)\\Offline;Database=DemoB;Integrated Security=true",
    ["QueryFilters:MandanteSupplierCode"]="1",
    ["QueryFilters:ExcludedClientCodes:0"]="2",
    ["QueryFilters:ExcludedClientCodes:1"]="3"
}).Build();
int count=0;
void Check(bool ok,string name) { if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++; }
Check(TenantConfiguration.GetConnectionString(config,"TENANT_A").Contains("DemoA"),"tenant A connection");
Check(TenantConfiguration.GetConnectionString(config,"TENANT_B").Contains("DemoB"),"tenant B connection");
try { TenantConfiguration.GetConnectionString(config,"invalid");throw new Exception("unknown tenant accepted"); }
catch(ArgumentException){ Check(true,"unknown tenant rejected"); }
try { TenantConfiguration.GetConnectionString(new ConfigurationBuilder().Build(),"TENANT_A");throw new Exception("missing connection accepted"); }
catch(InvalidOperationException){ Check(true,"missing connection rejected"); }

var data=new DataTable();
foreach(var column in new[]{"AnnoProtocollo","NumeroProtocollo","ClienteCodice","SoggettoCodice","CodiceFiscale","PartitaIva","Denominazione","ComunePratica","CodiceSoggetto","CognomeRagioneSociale","NomeSoggetto","Indirizzo","NumeroCivico","Cap","ComuneResidenza","ProvinciaResidenza","CodiceFiscaleDatore","TestoAnnotazione","TipoIND","TipoCONT"})data.Columns.Add(column,typeof(string));
var row=data.NewRow();
foreach(DataColumn col in data.Columns)row[col]="demo";
row["AnnoProtocollo"]="2026";row["NumeroProtocollo"]="1";row["CodiceFiscale"]="CF-DEMO";row["PartitaIva"]="VAT-DEMO";row["TestoAnnotazione"]="TEL.: 0000000001 0000000002";
data.Rows.Add(row);
var filter=new ColonneFiltraggioService(new DemoModulo(),config);
foreach(var type in new[]{"VUT","VSA","BAN","CCL","CMO","VED","DP1"})
{
    var request=new RichiestaExcel{CodAcc=type,CodCor="1",Formato="CLIENTE"};
    var result=filter.FiltraColonne(data,request,"TENANT_A");
    Check(result.Rows[0]["Protocollo"].ToString()=="20261" && result.Rows[0]["Codice Fiscale"].ToString()=="CF-DEMO" && result.Rows[0]["Mandante"].ToString()=="TENANT_A",type+" renamed export columns");
    if(type=="VUT")Check(result.Rows[0]["TEL 1"].ToString()=="0000000001" && result.Rows[0]["TEL 2"].ToString()=="0000000002","telephone export");
    if(type=="VSA")Check(result.Rows[0]["Partita iva"].ToString()=="VAT-DEMO","VAT export");
}
row["CodiceFiscale"]="";
Check(filter.FiltraColonne(data,new RichiestaExcel{CodAcc="VSA",CodCor="2"},"TENANT_B").Rows[0]["Codice Fiscale"].ToString()=="VAT-DEMO","tax code fallback");
var heirs=filter.FiltraColonne(data,new RichiestaExcel{CodAcc="VSA",CodCor="1",Formato="EREDI"},"TENANT_B");
Check(heirs.Rows[0]["PrimoRigo"].ToString()!.Contains("DEMO-EREDE"),"heir export contract");

// Exercise SQL parameter construction without opening a database connection.
var service=new DatabaseService(config,Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseService>.Instance);
var dates=typeof(DatabaseService).GetMethod("AddDateParameters",BindingFlags.NonPublic|BindingFlags.Static)!;
using var command=new SqlCommand("SELECT * FROM Pratiche WHERE 1=1 GROUP BY ClienteCodice");
dates.Invoke(null,new object[]{command,new FormData{DaDataAff="20260901",DataAff="20260930"}});
Check(command.Parameters["@DaDataAff"].SqlDbType==SqlDbType.Int && (int)command.Parameters["@DataAff"].Value==20260930,"typed date parameters");
foreach(var pair in new[]{("invalid","20260930"),("20260930","20260901")})
{
    try { dates.Invoke(null,new object[]{new SqlCommand(),new FormData{DaDataAff=pair.Item1,DataAff=pair.Item2}});throw new Exception("invalid dates accepted"); }
    catch(TargetInvocationException ex) when(ex.InnerException is ArgumentException){Check(true,"invalid date range rejected");}
}
var exclusions=typeof(DatabaseService).GetMethod("AddClientExclusions",BindingFlags.NonPublic|BindingFlags.Instance)!;
exclusions.Invoke(service,new object[]{command});
Check(command.CommandText.Contains("NOT IN (@ExcludedClient0, @ExcludedClient1)") && command.CommandText.IndexOf("NOT IN")<command.CommandText.IndexOf("GROUP BY")
    && (int)command.Parameters["@ExcludedClient0"].Value==2,"parameterized exclusions before grouping");
Console.WriteLine($"Completed {count} offline contract checks.");

sealed class DemoModulo:IModuloService
{
    public string LeggiModuloTesto(string a,string b,string tenant)=>"DEMO-NOTA";
    public string LeggiEredi(string a,string first,string tenant)=>first+"DEMO-EREDE";
    public void AggiornaFileCorrispondenti(int a,int b,string c,string d,string e,string f,string g,string tenant){}
}

