using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Data;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace CreazioneListe.Services
{
    public class ExcelService : IExcelService
    {
        private readonly IConfiguration _configuration;

        public ExcelService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<FileInfo>> CreateExcelFilesAsync(List<DataTable> dataTables, List<RichiestaExcel> richiesteExcel, string tenant)
        {
            // Definisci il percorso della directory in base al valore del tenant
            var baseDirectory = Path.Combine("C:\\Users\\Utente\\Desktop\\EXCEL", tenant);
            var directoryPath = Path.Combine(baseDirectory, DateTime.Now.ToString("yyyyMMdd"));

            // Crea la directory se non esiste
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var files = new List<FileInfo>();
            for (int i = 0; i < dataTables.Count; i++)
            {
                var richiesta = richiesteExcel[i];
                var fileName = $"{richiesta.NazCor}-{richiesta.CodCor}_{tenant}_{richiesta.CodAcc}_{richiesta.TotRic}_{richiesta.CodUrg}_{DateTime.Now:yyyyMMdd_HHmmssfff}.xlsx";
                var filePath = Path.Combine(directoryPath, fileName);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Dati");
                    var dataTable = dataTables[i];

                    // Aggiungi intestazioni con personalizzazione colore e stile
                    for (int col = 0; col < dataTable.Columns.Count; col++)
                    {
                        var cell = worksheet.Cells[1, col + 1];
                        cell.Value = dataTable.Columns[col].ColumnName;
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                        cell.Style.Font.Color.SetColor(Color.Black);
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        // Imposta il formato delle celle come Testo
                        worksheet.Column(col + 1).Style.Numberformat.Format = "@";
                    }

                    // Aggiungi i dati al file Excel
                    for (int row = 0; row < dataTable.Rows.Count; row++)
                    {
                        for (int col = 0; col < dataTable.Columns.Count; col++)
                        {
                            worksheet.Cells[row + 2, col + 1].Value = dataTable.Rows[row][col];
                        }
                    }

                    // Adatta automaticamente la larghezza delle colonne in base ai dati
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Salva il file Excel
                    await package.SaveAsAsync(new FileInfo(filePath));
                }

                files.Add(new FileInfo(filePath));
            }

            return await Task.FromResult(files);
        }
    }
}
