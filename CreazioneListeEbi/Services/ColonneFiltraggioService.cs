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

        public DataTable FiltraColonne(DataTable dataTable, RichiestaExcel richiesta)
        {
            var dataTableFiltrato = new DataTable();

            if (richiesta.CodCor == "8033")
            {
                dataTableFiltrato.Columns.Add("Azienda", typeof(string));
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

                if (richiesta.CodCor == "8033")
                {
                    newRow["Azienda"] = "EBI";
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

                // Se PBACFI è vuoto, utilizza PBAPIV
                if (dataTable.Columns.Contains("PBACFI") && !string.IsNullOrWhiteSpace(row["PBACFI"].ToString()))
                {
                    codiceFiscale = row["PBACFI"].ToString();
                }
                else if (dataTable.Columns.Contains("PBAPIV") && !string.IsNullOrWhiteSpace(row["PBAPIV"].ToString()))
                {
                    codiceFiscale = row["PBAPIV"].ToString();
                }

                // Se il codice fiscale ha lunghezza 11, aggiungi un apice all'inizio
                /*if (codiceFiscale.Length == 11)
                {
                    codiceFiscale = "'" + codiceFiscale;
                }*/

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
                        primoRigo += _moduloService.LeggiModuloTesto(row["PBAOGG"].ToString(), "003", "EBI");
                    }

                    if (dataTable.Columns.Contains("IBACOG"))
                    {
                        var erediHtml = _moduloService.LeggiEredi(row["IBACOG"].ToString(), primoRigo, "EBI");
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
