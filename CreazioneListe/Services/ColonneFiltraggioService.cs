using System.Data;
using CreazioneListe.Interfaces;
using CreazioneListe.Models;

namespace CreazioneListe.Services
{
    public class ColonneFiltraggioService : IColonneFiltraggioService
    {
        private readonly IModuloService _moduloService;

        public ColonneFiltraggioService(IModuloService moduloService)
        {
            _moduloService = moduloService;
        }

        public DataTable FiltraColonne(DataTable dataTable, RichiestaExcel richiesta, string tenant)
        {
            var dataTableFiltrato = new DataTable();

            // Aggiungi la colonna "Azienda" solo se richiesta da CodCor = "8033" e specifica il valore del tenant
            if (richiesta.CodCor == "8033")
            {
                dataTableFiltrato.Columns.Add("Mandante", typeof(string));
            }

            // Aggiungi le colonne principali desiderate
            dataTableFiltrato.Columns.Add("Protocollo", typeof(string));
            dataTableFiltrato.Columns.Add("Codice Fiscale", typeof(string));

            if (richiesta.Formato == "CLIENTE" && dataTable.Columns.Contains("PBACLI"))
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

                // Imposta il valore "Azienda" basato sul tenant se CodCor = "8033"
                if (richiesta.CodCor == "8033")
                {
                    newRow["Mandante"] = tenant == "EBI" ? "EBI" : "SSC";
                }

                // Verifica se le colonne esistono nel DataTable originale prima di accedervi
                if (dataTable.Columns.Contains("PBAANP") && dataTable.Columns.Contains("PBANUP"))
                {
                    newRow["Protocollo"] = row["PBAANP"].ToString() + row["PBANUP"].ToString();
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
                    if (dataTable.Columns.Contains("PBAPIV"))
                    {
                        newRow["Partita iva"] = row["PBAPIV"]?.ToString().Trim(); // Rimuove spazi superflui
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

                    if (dataTable.Columns.Contains("IC6TES"))
                    {
                        string ic6tesValue = row["IC6TES"]?.ToString().Trim();

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

                // Se PBACFI è vuoto, utilizza PBAPIV come Codice Fiscale
                if (dataTable.Columns.Contains("PBACFI") && !string.IsNullOrWhiteSpace(row["PBACFI"].ToString()))
                {
                    codiceFiscale = row["PBACFI"].ToString().Trim();
                }
                else if (dataTable.Columns.Contains("PBAPIV") && !string.IsNullOrWhiteSpace(row["PBAPIV"].ToString()))
                {
                    codiceFiscale = row["PBAPIV"].ToString().Trim();
                }

                // Assegna il valore al Codice Fiscale
                newRow["Codice Fiscale"] = string.IsNullOrEmpty(codiceFiscale) ? "N/A" : codiceFiscale;

                // Popola la colonna CLIENTE solo se richiesto
                if (richiesta.Formato == "CLIENTE" && dataTable.Columns.Contains("PBACLI"))
                {
                    newRow["CLIENTE"] = row["PBACLI"].ToString();
                }

                if (richiesta.CodAcc == "BAN" || richiesta.CodAcc == "CCL")
                {
                    if (!dataTableFiltrato.Columns.Contains("Denominazione"))
                    {
                        dataTableFiltrato.Columns.Add("Denominazione", typeof(string));
                    }
                    newRow["Denominazione"] = row["PBADEN"].ToString();
                    string columnName = $"ESITO";
                    if (!dataTableFiltrato.Columns.Contains(columnName))
                    {
                        dataTableFiltrato.Columns.Add(columnName, typeof(string));
                    }
                    newRow[columnName] = "";
                }

                if (richiesta.CodAcc == "CMO")
                {
                    if (!dataTableFiltrato.Columns.Contains("PBADEN"))
                    {
                        dataTableFiltrato.Columns.Add("PBADEN", typeof(string));
                    }
                    if (!dataTableFiltrato.Columns.Contains("PBACIT"))
                    {
                        dataTableFiltrato.Columns.Add("PBACIT", typeof(string));
                    }
                    newRow["PBADEN"] = row["PBADEN"].ToString();
                    newRow["PBACIT"] = row["PBACIT"].ToString();
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
                    newRow["P.Iva"] = row["LAVCFI"].ToString();
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

                    if (dataTable.Columns.Contains("IBARS1") && dataTable.Columns.Contains("IBARS2"))
                    {
                        primoRigo += $"{richiesta.CodAcc} {row["IBARS1"]} {row["IBARS2"]} ";
                    }

                    if (dataTable.Columns.Contains("TipoIND") && dataTable.Columns.Contains("IBADEI") && dataTable.Columns.Contains("IBACII"))
                    {
                        primoRigo += $"{row["TipoIND"]} {row["IBADEI"]} {row["IBACII"]} ";
                    }

                    if (dataTable.Columns.Contains("IBACAP") && dataTable.Columns.Contains("IBACIT"))
                    {
                        primoRigo += $"{row["IBACAP"]} {row["IBACIT"]} ";
                    }

                    if (dataTable.Columns.Contains("IBAPRV"))
                    {
                        primoRigo += $"{row["IBAPRV"]} ";
                    }

                    if (dataTable.Columns.Contains("PBAOGG"))
                    {
                        primoRigo += _moduloService.LeggiModuloTesto(row["PBAOGG"].ToString(), "003", tenant);
                    }

                    if (dataTable.Columns.Contains("IBACOG"))
                    {
                        var erediHtml = _moduloService.LeggiEredi(row["IBACOG"].ToString(), primoRigo, tenant);
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
