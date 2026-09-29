# CreazioneListe

Applicazione web per preparare liste operative a partire dalle pratiche presenti in SQL Server. Permette di consultare le assegnazioni, selezionare gruppi di pratiche e produrre file Excel destinati ai fornitori, mantenendo separati due ambienti identificati come **TENANT_A** e **TENANT_B**.

## A cosa serve

- Consultare conteggi e raggruppamenti per fornitore, tipo di accertamento e urgenza.
- Selezionare le lavorazioni da esportare e generare Excel con i campi previsti dal servizio.
- Scaricare file e archivi ZIP e consultare il registro delle esportazioni.
- Trasferire file tramite SFTP oppure inviarli a un'integrazione HTTP configurabile.

È uno strumento per un flusso di lavoro basato su tracciati specifici: per collegarlo a un altro gestionale occorre adattare il contratto SQL e verificare i campi esportati.

## Tecnologie e requisiti

ASP.NET Core MVC/API su **.NET 8**, SQL Server, Microsoft.Data.SqlClient, EPPlus per Excel, SSH.NET per SFTP, Serilog e Swagger.

Servono il .NET SDK compatibile con `net8.0`, un database SQL Server con lo schema previsto e cartelle scrivibili per gli output. SFTP e integrazione HTTP servono solo per le relative funzionalità.

## Configurazione

I comandi seguenti si eseguono dalla radice del repository. Le impostazioni riservate vanno in User Secrets durante lo sviluppo o nelle variabili d'ambiente del processo.

| Chiave | Utilizzo |
| --- | --- |
| `ConnectionStrings:DefaultConnection_TENANT_A` | Database del primo tenant |
| `ConnectionStrings:DefaultConnection_TENANT_B` | Database del secondo tenant |
| `Paths:TENANT_A`, `Paths:TENANT_B` | Directory di lavoro dei file |
| `Sftp:Host`, `Sftp:Port`, `Sftp:Username`, `Sftp:Password` | Connessione SFTP; porta predefinita 22 |
| `Integrations:UploadUrl` | Endpoint HTTP che riceve i file |
| `QueryFilters:ExcludedClientCodes` | Elenco facoltativo dei codici cliente da escludere |
| `QueryFilters:MandanteSupplierCode` | Codice fornitore per la regola della colonna Mandante |
| `Exports:Operator` | Valore facoltativo dell'operatore negli export |

Esempio di configurazione locale in PowerShell, con database dimostrativi e autenticazione Windows:

~~~powershell
dotnet user-secrets set --project CreazioneListe/CreazioneListe.csproj "ConnectionStrings:DefaultConnection_TENANT_A" "Server=(localdb)\MSSQLLocalDB;Database=ListeDemoA;Trusted_Connection=True;"
dotnet user-secrets set --project CreazioneListe/CreazioneListe.csproj "ConnectionStrings:DefaultConnection_TENANT_B" "Server=(localdb)\MSSQLLocalDB;Database=ListeDemoB;Trusted_Connection=True;"
dotnet user-secrets set --project CreazioneListe/CreazioneListe.csproj "Paths:TENANT_A" "C:\demo\liste\tenant-a"
dotnet user-secrets set --project CreazioneListe/CreazioneListe.csproj "Paths:TENANT_B" "C:\demo\liste\tenant-b"
~~~

Creare le cartelle e predisporre i database prima di utilizzare le funzioni. Per le variabili d'ambiente sostituire `:` con `__`; un elemento di un array si indica, ad esempio, con `QueryFilters__ExcludedClientCodes__0`.

### Contratto del database

[database/schema.sql](database/schema.sql) contiene uno schema dimostrativo vuoto. Definisce nomi generici coerenti, tra cui `Pratiche`, `Assegnazioni`, `Fornitori`, `TipiAccertamento`, `Soggetti` e `RegistroFile`.

Lo script non viene eseguito automaticamente e non è una migrazione di un database esistente. Per dati reali, predisporre tabelle o viste compatibili con il contratto del servizio; tenere gli adattatori specifici e i dati fuori dal repository.

## Avvio

~~~powershell
dotnet restore CreazioneListe.sln
dotnet build CreazioneListe.sln
dotnet run --project CreazioneListe/CreazioneListe.csproj --launch-profile CreazioneListe
~~~

Aprire `https://localhost:7255`; il profilo espone anche `http://localhost:5030`. In sviluppo la documentazione API è disponibile su `/swagger`. Per HTTPS locale, configurare il certificato di sviluppo con `dotnet dev-certs https --trust`.

## Utilizzo

1. Scegliere TENANT_A oppure TENANT_B nell'interfaccia.
2. Impostare l'intervallo richiesto dalla schermata; dove il campo è testuale, le date sono nel formato `yyyyMMdd`.
3. Consultare i gruppi restituiti e selezionare quelli da elaborare.
4. Generare il file Excel e controllarne intestazioni e contenuto.
5. Scaricare i file oppure utilizzare il trasferimento SFTP o l'invio HTTP, dopo aver configurato la destinazione.
6. Consultare il registro per verificare le operazioni registrate.

Il risultato dipende dai dati presenti e dalle regole del tipo di accertamento. Un database vuoto consente di verificare il contratto, ma non produce liste rappresentative.

## Verifiche e struttura

~~~powershell
dotnet run --project tests/Contracts/Contracts.csproj
~~~

I controlli di contratto non richiedono un database. Per il test SQL con database dedicato vedere [tests/README.md](tests/README.md).

- [CreazioneListe](CreazioneListe): applicazione, controller, viste e servizi.
- [database/schema.sql](database/schema.sql): contratto SQL dimostrativo.
- [PUBLICATION.md](PUBLICATION.md): configurazione esterna e indicazioni per la pubblicazione.
