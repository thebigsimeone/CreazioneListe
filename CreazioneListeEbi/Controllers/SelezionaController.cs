using CreazioneListeEbi.Interfaces;
using CreazioneListeEbi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace CreazioneListeEbi.Controllers
{
    public class SelezionaController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly IExcelService _excelService;

        public SelezionaController(IDatabaseService databaseService, IExcelService excelService)
        {
            _databaseService = databaseService;
            _excelService = excelService;
        }

        public async Task<IActionResult> Seleziona(FormData formData)
        {
            // Ottiene i dati dal database in base al form di input fornito dall'utente.
            var data = await _databaseService.GetDataAsync(formData);
            return View(data);
        }

        public async Task<IActionResult> Crea(FormData formData)
        {
            // Ottiene i dati dal database in base al form di input
            var data = await _databaseService.GetDataAsync(formData);

            // Creiamo un oggetto RichiestaExcel basato sui dati di input forniti e sui dati recuperati
            var richiesta = new RichiestaExcel
            {
                NazCor = formData.NazCor,
                CodCor = formData.CodCor,
                CodAcc = formData.CodAcc,
                CodUrg = data.Rows.Count > 0 ? data.Rows[0]["PBSURG"].ToString() : null,
                Formato = formData.CodAcc,  // Questo è un esempio; cambia a seconda della logica specifica
                TotRic = data.Rows.Count
            };

            // Creiamo il file Excel utilizzando il servizio di creazione Excel
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files");
            var filePath = await _excelService.CreateExcelFileAsync(richiesta);

            return RedirectToAction("ListaFile", "File");
        }
        public async Task<IActionResult> TestQuery(string selectedRows)
        {
            if (string.IsNullOrEmpty(selectedRows))
            {
                return BadRequest("Nessuna riga selezionata.");
            }

            // Parsing dei dati selezionati
            var datiSelezionati = selectedRows.Split(',');
            var richiestaExcel = new RichiestaExcel
            {
                NazCor = datiSelezionati.Length > 0 ? datiSelezionati[0] : null,
                CodCor = datiSelezionati.Length > 1 ? datiSelezionati[1] : null,
                CodAcc = datiSelezionati.Length > 2 ? datiSelezionati[2] : null,
                CodUrg = datiSelezionati.Length > 3 ? datiSelezionati[3] : null
            };

            // Assegna valori costanti per DaDataAff e DataAff
            var daDataAff = DateTime.Now.AddDays(-7).ToString("yyyyMMdd");
            var dataAff = DateTime.Now.ToString("yyyyMMdd");

            var formData = new FormData
            {
                DaDataAff = daDataAff,
                DataAff = dataAff,
                NazCor = richiestaExcel.NazCor,
                CodCor = richiestaExcel.CodCor,
                CodAcc = richiestaExcel.CodAcc
            };

            // Recupera tutti i dati dal database
            var data = await _databaseService.GetDataTestAsync(richiestaExcel, formData);

            // Filtra e rielabora le colonne che vuoi visualizzare
            var dataFiltrata = FiltraColonne(data);

            return View("~/Views/Test/VisualizzaDati.cshtml", dataFiltrata);
        }

        // Metodo per filtrare e rielaborare le colonne del DataTable
        private DataTable FiltraColonne(DataTable dataTable)
        {
            var dataTableFiltrato = new DataTable();

            // Aggiungi le colonne desiderate
            dataTableFiltrato.Columns.Add("Protocollo", typeof(string));
            dataTableFiltrato.Columns.Add("PBACFI", typeof(string));
            dataTableFiltrato.Columns.Add("PBACLI", typeof(string));

            // Copia i dati per le colonne selezionate
            foreach (DataRow row in dataTable.Rows)
            {
                var newRow = dataTableFiltrato.NewRow();
                newRow["Protocollo"] = row["PBAANP"].ToString() + row["PBANUP"].ToString();
                newRow["PBACFI"] = row["PBACFI"].ToString();
                newRow["PBACLI"] = row["PBACLI"].ToString();
                dataTableFiltrato.Rows.Add(newRow);
            }

            return dataTableFiltrato;
        }
    }
}
