# Configurazione privata

Le credenziali e i dati operativi devono restare fuori dal repository. Le impostazioni ASP.NET Core si configurano tramite variabili di ambiente (separatore doppio underscore) oppure dotnet user-secrets in sviluppo, indicando il progetto con --project. I valori vuoti nei file di esempio devono essere configurati prima di utilizzare i servizi corrispondenti. I file .env non vengono caricati automaticamente.

Non includere backup, esportazioni, log, chiavi private o dati personali nei commit.

La bonifica dei file correnti non elimina i valori presenti nella cronologia Git: prima di rendere pubblico il repository, revocare o ruotare le credenziali già versionate e bonificare la cronologia, gli altri branch/tag e gli eventuali allegati.

Configurare ConnectionStrings__DefaultConnection_TENANT_A, ConnectionStrings__DefaultConnection_TENANT_B, Paths__TENANT_A, Paths__TENANT_B, Sftp__Host, Sftp__Port, Sftp__Username, Sftp__Password e Integrations__UploadUrl. QueryFilters__ExcludedClientCodes__0, __1, ecc. impostano le esclusioni dei clienti; QueryFilters__MandanteSupplierCode abilita la colonna Mandante per il fornitore configurato. Nessun codice cliente reale viene distribuito.

Le query, i servizi, i controller e le viste condividono uno schema dimostrativo con nomenclature descrittive. database/schema.sql definisce il contratto SQL Server vuoto. Per un database preesistente occorre predisporre viste o adattatori privati equivalenti; non eseguire questo script sul database di produzione. Le rotte e i tenant sono ora SelezionaTenantA, SelezionaTenantB, TENANT_A e TENANT_B.

Exports__Operator imposta l’operatore riportato nel registro; senza configurazione il campo resta vuoto. Le directory SFTP nei menu sono esempi FORNITORE_A/B da adattare nella distribuzione privata.
