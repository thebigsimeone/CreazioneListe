using System.Data;
using CreazioneListe.Interfaces;
using CreazioneListe.Models;

namespace CreazioneListe.Services
{
    public class ColonneFiltraggioService : IColonneFiltraggioService
    {
        private readonly IModuloService _moduloService;
        private readonly IConfiguration _configuration;

        public ColonneFiltraggioService(IModuloService moduloService, IConfiguration configuration)
        {
            _moduloService = moduloService;
            _configuration = configuration;
        }

        public DataTable FiltraColonne(DataTable dataTable, RichiestaExcel richiesta, string tenant)
        {
            var dataTableFiltrato = new DataTable();

            // Aggiungi la colonna "Azienda" solo se richiesta da CodCor = il fornitore configurato e specifica il valore del tenant
            if (!string.IsNullOrWhiteSpace(_configuration["QueryFilters:MandanteSupplierCode"]) && richiesta.CodCor == _configuration["QueryFilters:MandanteSupplierCode"])
            {
                dataTableFiltrato.Columns.Add("Mandante", typeof(string));
            }

            // Aggiungi le colonne principali desiderate
            dataTableFiltrato.Columns.Add("Protocollo", typeof(string));
            dataTableFiltrato.Columns.Add("Codice Fiscale", typeof(string));

            if (richiesta.Formato == "CLIENTE" && dataTable.Columns.Contains("ClienteCodice"))
            {
                if (!dataTableFiltrato.Columns.Contains("CLIENTE"))
                {
                    dataTableFiltrato.Columns.Add("CLIENTE", typeof(string));
                }
            }

            if (richiesta.Formato == "EREDI")
            {
                dataTableFiltrato.Columns.Add("PrimoRigo", typeof(string));
            }

            foreach (DataRow row in dataTable.Rows)
            {
                var newRow = dataTableFiltrato.NewRow();

                // Imposta il valore "Azienda" basato sul tenant se CodCor = il fornitore configurato
                if (!string.IsNullOrWhiteSpace(_configuration["QueryFilters:MandanteSupplierCode"]) && richiesta.CodCor == _configuration["QueryFilters:MandanteSupplierCode"])
                {
                    newRow["Mandante"] = tenant == "TENANT_A" ? "TENANT_A" : "TENANT_B";
                }

                // Verifica se le colonne esistono nel DataTable originale prima di accedervi
                if (dataTable.Columns.Contains("AnnoProtocollo") && dataTable.Columns.Contains("NumeroProtocollo"))
                {
                    newRow["Protocollo"] = row["AnnoProtocollo"].ToString() + row["NumeroProtocollo"].ToString();
                }
                else
                {
                    newRow["Protocollo"] = "N/A";
                }

                string codiceFiscale = string.Empty;

                // Aggiungi la colonna "Partita Iva" se CodAcc è "VSA"
                if (richiesta.CodAcc == "VSA")
                {
                    if (!dataTableFiltrato.Columns.Contains("Partita iva"))
                    {
                        dataTableFiltrato.Columns.Add("Partita iva", typeof(string));
                    }
                    if (dataTable.Columns.Contains("PartitaIva"))
                    {
                        newRow["Partita iva"] = row["PartitaIva"]?.ToString().Trim(); // Rimuove spazi superflui
                    }
                }

                if (richiesta.CodAcc == "VUT")
                {
                    if (!dataTableFiltrato.Columns.Contains("TEL 1"))
                    {
                        dataTableFiltrato.Columns.Add("TEL 1", typeof(string));
                    }
                    if (!dataTableFiltrato.Columns.Contains("TEL 2"))
                    {
                        dataTableFiltrato.Columns.Add("TEL 2", typeof(string));
                    }
                    if (!dataTableFiltrato.Columns.Contains("ATTIVO 1"))
                    {
                        dataTableFiltrato.Columns.Add("ATTIVO 1", typeof(string));
                    }
                    if (!dataTableFiltrato.Columns.Contains("ATTIVO 2"))
                    {
                        dataTableFiltrato.Columns.Add("ATTIVO 2", typeof(string));
                    }

                    if (dataTable.Columns.Contains("TestoAnnotazione"))
                    {
                        string ic6tesValue = row["TestoAnnotazione"]?.ToString().Trim();

                        if (!string.IsNullOrEmpty(ic6tesValue))
                        {
                            // Estrai tutti i numeri validi separati da spazio, virgola o altro
                            var numeri = ic6tesValue.Split(new[] { ' ', ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                                                    .Where(n => n.All(char.IsDigit)) // Considera solo stringhe numeriche
                                                    .ToList();

                            if (numeri.Count > 0)
                            {
                                newRow["TEL 1"] = numeri[0];
                            }
                            if (numeri.Count > 1)
                            {
                                newRow["TEL 2"] = numeri[1];
                            }
                        }
                    }
                }

                // Se CodiceFiscale è vuoto, utilizza PartitaIva come Codice Fiscale
                if (dataTable.Columns.Contains("CodiceFiscale") && !string.IsNullOrWhiteSpace(row["CodiceFiscale"].ToString()))
                {
                    codiceFiscale = row["CodiceFiscale"].ToString().Trim();
                }
                else if (dataTable.Columns.Contains("PartitaIva") && !string.IsNullOrWhiteSpace(row["PartitaIva"].ToString()))
                {
                    codiceFiscale = row["PartitaIva"].ToString().Trim();
                }

                // Assegna il valore al Codice Fiscale
                newRow["Codice Fiscale"] = string.IsNullOrEmpty(codiceFiscale) ? "N/A" : codiceFiscale;

                // Popola la colonna CLIENTE solo se richiesto
                if (richiesta.Formato == "CLIENTE" && dataTable.Columns.Contains("ClienteCodice"))
                {
                    newRow["CLIENTE"] = row["ClienteCodice"].ToString();
                }

                if (richiesta.CodAcc == "BAN" || richiesta.CodAcc == "CCL")
                {
                    if (!dataTableFiltrato.Columns.Contains("Denominazione"))
                    {
                        dataTableFiltrato.Columns.Add("Denominazione", typeof(string));
                    }
                    newRow["Denominazione"] = row["Denominazione"].ToString();
                    string columnName = $"ESITO";
                    if (!dataTableFiltrato.Columns.Contains(columnName))
                    {
                        dataTableFiltrato.Columns.Add(columnName, typeof(string));
                    }
                    newRow[columnName] = "";
                }

                if (richiesta.CodAcc == "CMO")
                {
                    if (!dataTableFiltrato.Columns.Contains("Denominazione"))
                    {
                        dataTableFiltrato.Columns.Add("Denominazione", typeof(string));
                    }
                    if (!dataTableFiltrato.Columns.Contains("ComunePratica"))
                    {
                        dataTableFiltrato.Columns.Add("ComunePratica", typeof(string));
                    }
                    newRow["Denominazione"] = row["Denominazione"].ToString();
                    newRow["ComunePratica"] = row["ComunePratica"].ToString();
                }

                if (richiesta.CodAcc == "VED")
                {
                    if (!dataTableFiltrato.Columns.Contains("P.Iva"))
                    {
                        dataTableFiltrato.Columns.Add("P.Iva", typeof(string));
                    }
                    if (!dataTableFiltrato.Columns.Contains("ESITO"))
                    {
                        dataTableFiltrato.Columns.Add("ESITO", typeof(string));
                    }
                    newRow["P.Iva"] = row["CodiceFiscaleDatore"].ToString();
                    newRow["ESITO"] = "";
                }

                if (richiesta.CodAcc == "DP1")
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        string columnName = $"ESITO {i}";
                        if (!dataTableFiltrato.Columns.Contains(columnName))
                        {
                            dataTableFiltrato.Columns.Add(columnName, typeof(string));
                        }
                        newRow[columnName] = "";
                    }
                }

                if (richiesta.Formato == "EREDI")
                {
                    var primoRigo = string.Empty;

                    if (dataTable.Columns.Contains("CognomeRagioneSociale") && dataTable.Columns.Contains("NomeSoggetto"))
                    {
                        primoRigo += $"{richiesta.CodAcc} {row["CognomeRagioneSociale"]} {row["NomeSoggetto"]} ";
                    }

                    if (dataTable.Columns.Contains("TipoIND") && dataTable.Columns.Contains("Indirizzo") && dataTable.Columns.Contains("NumeroCivico"))
                    {
                        primoRigo += $"{row["TipoIND"]} {row["Indirizzo"]} {row["NumeroCivico"]} ";
                    }

                    if (dataTable.Columns.Contains("Cap") && dataTable.Columns.Contains("ComuneResidenza"))
                    {
                        primoRigo += $"{row["Cap"]} {row["ComuneResidenza"]} ";
                    }

                    if (dataTable.Columns.Contains("ProvinciaResidenza"))
                    {
                        primoRigo += $"{row["ProvinciaResidenza"]} ";
                    }

                    if (dataTable.Columns.Contains("SoggettoCodice"))
                    {
                        primoRigo += _moduloService.LeggiModuloTesto(row["SoggettoCodice"].ToString(), "003", tenant);
                    }

                    if (dataTable.Columns.Contains("CodiceSoggetto"))
                    {
                        var erediHtml = _moduloService.LeggiEredi(row["CodiceSoggetto"].ToString(), primoRigo, tenant);
                        newRow["PrimoRigo"] = erediHtml;
                    }
                    else
                    {
                        newRow["PrimoRigo"] = primoRigo;
                    }
                }

                dataTableFiltrato.Rows.Add(newRow);
            }

            return dataTableFiltrato;
        }
    }
}

