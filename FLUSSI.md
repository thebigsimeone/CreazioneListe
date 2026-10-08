# CreazioneListe — Flussi operativi

[README del progetto](README.md) · [Flussi nel README](README.md#flussi-operativi)

I flussi descrivono il comportamento implementato, inclusi gli effetti parziali e le automazioni non attive. La ricostruzione si basa sull'analisi statica dei sorgenti dell'8 ottobre 2026; le verifiche proposte non costituiscono test già eseguiti.

## Contesto operativo

L'operatore usa l'interfaccia MVC; un'integrazione può usare le API. Prima delle operazioni devono esistere database, percorsi dei tenant ed eventuali destinazioni SFTP/HTTP configurate.

Dalla home l'utente sceglie **TENANT_A** o **TENANT_B**, apre la selezione, genera le liste e passa alla pagina dei file. Non risulta un flusso di autenticazione nei sorgenti esaminati.

## Consultazione dei gruppi

1. L'utente apre il modulo del tenant, indica l'intervallo di affidamento e invia.
2. `Submit` reindirizza all'azione di selezione con i parametri.
3. `DatabaseService.GetSelectAsync` sceglie la connessione del tenant, esegue la query e raggruppa per fornitore, accertamento e urgenza.
4. Il filtro comprende assegnazioni in stato `2`, non esportate (`Esportata <> 'S'`), pratiche non `SCO` e le ulteriori condizioni della query/configurazione.
5. Restituisce una tabella con conteggi; la vista mostra i gruppi.
6. L'utente sceglie le righe da esportare, il formato e l'eventuale unione. Con nessun gruppo trovato non ci sono lavorazioni da selezionare.

Riferimenti: [tenant A](CreazioneListe/Controllers/SelezionaTenantAController.cs), [tenant B](CreazioneListe/Controllers/SelezionasTenantBController.cs), [query](CreazioneListe/Services/DatabaseService.cs).

**Nota sul tenant B:** l'azione nel codice si chiama `SelezionaTENANT_B`, mentre `Submit` usa `SelezionaTenantB` nel redirect. Sono nomi differenti, non solo maiuscole/minuscole; il percorso va verificato in esecuzione prima di considerare completo il ciclo dall'interfaccia.

## Generazione dei file selezionati

1. L'utente invia le righe selezionate, i formati e le opzioni “unisci”.
2. Il JavaScript aggiunge i parametri dinamici del modulo. “Seleziona tutte” cambia i checkbox e abilita i relativi campi.
3. `CreaFile` rifiuta una selezione vuota con 400.
4. Per ogni gruppo ricava nazione/codice fornitore, accertamento e urgenza; costruisce `RichiestaExcel`.
5. **Nell'interfaccia, questa fase ricrea l'intervallo da oggi meno 7 giorni a oggi**, secondo il server, anziché riprendere quello del modulo iniziale.
6. Esegue `GetSelectedAsync`, conta le righe e chiama `FiltraColonne`.
7. Le righe marcate “unisci” vengono accodate per **CodAcc**; le altre producono file separati. Il metadato della richiesta unita deriva dalla prima richiesta del gruppo.
8. `ExcelService` crea la directory del giorno sotto `Paths:TENANT_A` o `Paths:TENANT_B`, genera i fogli, scrive gli XLSX e registra ogni file su SQL.
9. Il controller restituisce la vista con i file generati.
10. L'utente può scaricare, inviare o consultare il registro.

### Trasformazioni automatiche

| Regola | Effetto |
| --- | --- |
| Tutti i tracciati | Protocollo da anno + numero; CF oppure P.IVA come fallback, altrimenti N/A |
| Fornitore configurato | Colonna Mandante |
| Formato CLIENTE | Colonna CLIENTE, se presente nella query |
| Formato EREDI | Costruzione PrimoRigo, lettura annotazioni e dati eredi |
| VSA | Partita iva |
| VUT | TEL 1/TEL 2 estratti dalle annotazioni e colonne ATTIVO |
| BAN/CCL | Denominazione ed ESITO |
| CMO | Denominazione e ComunePratica |
| VED | P.Iva ed ESITO |
| DP1 | Cinque colonne ESITO |

Riferimenti: [filtro colonne](CreazioneListe/Services/ColonneFiltraggioService.cs), [moduli](CreazioneListe/Services/ModuloService.cs), [Excel](CreazioneListe/Services/ExcelService.cs), [registro](CreazioneListe/Services/RegistroFileService.cs).

Il nome file include fornitore, tenant, servizio, data/ora e numero righe. Se esiste già, viene aggiunto un suffisso numerico. Il registro conserva data, ora, intervallo, fornitore, servizio, urgenza, conteggio, formato, percorso e operatore configurato.

**Effetti parziali:** il salvataggio file precede l'inserimento nel registro; non c'è una transazione comune a disco e SQL. Un errore nel registro può lasciare il file già creato, e un errore su un file successivo può lasciare gli output precedenti.

**Automazione non attiva:** `AggiornaFileCorrispondenti` contiene la logica per impostare `Esportata='S'` e il nome export, ma le chiamate nei controller di generazione sono commentate. La generazione del file non aggiorna quindi automaticamente lo stato delle assegnazioni.

## Download locali e registro

| Azione iniziale | Operazione server | Risposta |
| --- | --- | --- |
| Download singolo dalla lista | Legge il file nella directory di oggi del tenant | File XLSX |
| DownloadAll | Elenca i file della directory di oggi e crea uno ZIP sul disco | Archivio scaricabile |
| Registro per tenant/data | Legge RegistroFile, con dati del fornitore, filtrato per data di registrazione | Tabella dei file |
| ScaricaFile dal registro | Legge il file nella directory della data indicata | File |
| ScaricaTuttiFile dal registro | Crea ZIP dalla directory della data indicata | Archivio |

Il pulsante JavaScript `scaricaTutti()`, dove usato, avvia download singoli distanziati di 750 ms: è distinto dalle azioni server che producono ZIP. Gli ZIP creati sul disco rimangono nella cartella; non c'è pulizia automatica e gli archivi già presenti possono essere inclusi in archivi successivi.

Il registro non prova l'avvenuto invio SFTP/HTTP: viene scritto alla generazione. Un record può ancora esistere anche se il file su disco non è più disponibile. Riferimenti: [FileController](CreazioneListe/Controllers/FileController.cs), [RegistroFileController](CreazioneListe/Controllers/RegistroFileController.cs), [FileService](CreazioneListe/Services/FileService.cs), [JavaScript](CreazioneListe/wwwroot/js/site.js).

## Invio esterno: SFTP seguito da HTTP

1. L'utente seleziona file, tenant e directory remota e invia il modulo.
2. Il JavaScript intercetta il submit, mostra lo spinner ed esegue un POST a `File/SendFile`.
3. Il controller valida i parametri, converte la directory remota in maiuscolo e verifica che `Integrations:UploadUrl` sia HTTP/HTTPS assoluto.
4. Apre il file locale del giorno.
5. `SftpService` si connette, crea la directory remota se assente, carica il file e si disconnette.
6. Il controller riporta lo stream all'inizio e invia lo stesso file all'URL HTTP come multipart, campo **file**.
7. Solo se entrambi gli invii terminano correttamente restituisce 200.
8. Il browser nasconde lo spinner e mostra successo oppure il messaggio di errore.

```mermaid
flowchart TD
    A["Invia file"] --> B["Valida parametri e destinazione"]
    B --> C["Carica via SFTP"]
    C --> D{"SFTP riuscito?"}
    D -->|No| E["Errore; invio HTTP non eseguito"]
    D -->|Sì| F["Invia multipart HTTP"]
    F --> G{"HTTP riuscito?"}
    G -->|No| H["Errore; copia SFTP già presente"]
    G -->|Sì| I["200 e successo in pagina"]
```

Non è presente rollback SFTP se l'HTTP fallisce, né una coda di retry persistente o una chiave di deduplicazione. Ripetere un invio può quindi ripetere gli effetti esterni.

**Integrazione con AppComunicazioni:** `UploadUrl` è configurabile. Il codice di CreazioneListe usa il campo multipart `file`; l'API AppComunicazioni dichiara `File`. Il collegamento effettivo dipende dalla configurazione e va verificato. Inoltre AppComunicazioni legge il conteggio dall'ultimo segmento del nome: un suffisso anticollisione di CreazioneListe può essere interpretato come conteggio. La compatibilità del nome di esportazione deve pertanto essere verificata.

## API e consultazione SFTP

| Endpoint | Ingresso ed elaborazione | Fine |
| --- | --- | --- |
| GET `/api/SelectQueryApi/GetData` | Client invia FormData e tenant → verifica date obbligatorie → query aggregata → conversione tabella | JSON |
| GET `/api/SelectedQueryApi/GetSelectedDataToExcel` | Client invia date, fornitore, servizio, urgenza, formato e conteggio → query dettagli → filtro → creazione su disco e registro | Download del primo file generato |
| GET `/api/Sftp/list-directories` | Client indica remotePath → connessione e lista directory | JSON |
| GET `/api/Sftp/list-files` | Client indica remotePath → connessione e lista file | JSON |
| GET `/api/Sftp/download-file` | Client indica remoteFilePath → legge file remoto in memoria | Download |
| `RegistroFile/CustomDownload` | Utente indica cartella remota → lista e download dei file → ZIP in memoria | Archivio oppure 404 se vuoto |

**Il GET di generazione Excel ha effetti di scrittura** su disco e registro; non è una semplice consultazione. Il suo intervallo viene dai parametri del client, diversamente dalla generazione MVC.

## Errori, automazioni e fine del ciclo

Le API di query restituiscono 400 per date obbligatorie mancanti e 500 per errori di elaborazione. Tenant SQL non riconosciuto o connessione mancante vengono rifiutati da `TenantConfiguration`. Le azioni file distinguono alcuni 400/404; gli errori del registro e SFTP possono diventare 500. Il browser mostra gli errori dell'invio, ma non recupera automaticamente un invio parziale.

Non risultano processi schedulati, watcher, email o GitHub Actions. Query, trasformazioni, nomi, registrazione, ZIP e invio doppio sono automatici **in risposta all'azione utente/client**. Dopo la risposta non continua un job: restano file locali, record SQL ed eventuali copie remote.

## Verifica dei flussi

Con database e destinazioni di prova: verificare entrambi i tenant, intervallo visualizzato e intervallo usato nella generazione, unione per servizio, formato di ogni tracciato, nome anticollisione, registro e download per data. Simulare HTTP non disponibile dopo un SFTP riuscito e controllare l'effetto parziale prima di ripetere.
