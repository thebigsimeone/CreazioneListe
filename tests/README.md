# Verifiche

Eseguire dalla radice del repository:

```powershell
dotnet run --project tests/Contracts/Contracts.csproj
```

I 19 controlli offline verificano tenant, configurazione mancante, filtri parametrizzati, date non valide e colonne usate dalle esportazioni.

Il progetto `tests/Smoke` esegue query reali, inserimenti nel registro e aggiornamenti su dati esclusivamente sintetici. Richiede un database SQL Server LocalDB **vuoto**, chiamato `PublicationAudit`, in un'istanza temporanea con prefisso `CodexPublicationAudit_`. Impostare `CREAZIONE_TEST_CONNECTION` con la connessione a quel database e avviare:

```powershell
dotnet run --project tests/Smoke/Smoke.csproj
```

Il test crea lo schema e inserisce dati: non accetta altre istanze né database di produzione. Al termine eliminare il database e l'istanza temporanei. Il database deve essere ricreato vuoto prima di ripetere il test.
