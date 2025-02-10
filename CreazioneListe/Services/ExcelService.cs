using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Data;
using System.Drawing;

namespace CreazioneListe.Services
{
    public class ExcelService : IExcelService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ExcelService> _logger;
        private readonly IRegistroFileService _registroFileService;

        public ExcelService(IConfiguration configuration, ILogger<ExcelService> logger, IRegistroFileService registroFileService)
        {
            _configuration = configuration;
            _logger = logger;
            _registroFileService = registroFileService;
        }

        public async Task<List<FileInfo>> CreateExcelFilesAsync(List<DataTable> dataTables, List<RichiestaExcel> richiesteExcel, FormData formData, string tenant)
        {
            var files = new List<FileInfo>();
            try
            {
                // Recupera il percorso base dal file di configurazione
                var baseDirectory = _configuration[$"Paths:{tenant.ToUpper()}"];
                if (string.IsNullOrEmpty(baseDirectory))
                    throw new ArgumentException($"Percorso non configurato per il tenant: {tenant}");

                var directoryPath = Path.Combine(baseDirectory, DateTime.Now.ToString("yyyyMMdd"));

                // Crea la directory se non esiste
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogInformation("Creazione della directory: {DirectoryPath}", directoryPath);
                    Directory.CreateDirectory(directoryPath);
                }

                // Ricava il percorso troncato per uso nel registro
                var truncatedPath = directoryPath.Replace(@"\\10.10.20.5\f\", @"F:\")
                                                 .Replace(@"\\10.10.12.5\f\", @"F:\");

                for (int i = 0; i < dataTables.Count; i++)
                {
                    var richiesta = richiesteExcel[i];
                    var dataTable = dataTables[i];

                    var baseFileName = $"{richiesta.NazCor}-{richiesta.CodCor}_{tenant}_{richiesta.CodAcc}_{DateTime.Now:yyyyMMdd-HHmm}_{richiesta.TotRic}";
                    var fileName = $"{baseFileName}.xlsx";
                    var filePath = Path.Combine(directoryPath, fileName);
                    int fileIndex = 1;

                    // Evita sovrascritture aggiungendo un suffisso al nome del file
                    while (File.Exists(filePath))
                    {
                        fileName = $"{baseFileName}_{fileIndex}.xlsx";
                        filePath = Path.Combine(directoryPath, fileName);
                        fileIndex++;
                    }

                    _logger.LogInformation("Creazione del file Excel: {FilePath}", filePath);

                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Dati");


                        // Aggiungi intestazioni
                        for (int col = 0; col < dataTable.Columns.Count; col++)
                        {
                            var cell = worksheet.Cells[1, col + 1];
                            cell.Value = dataTable.Columns[col].ColumnName;
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            worksheet.Column(col + 1).Style.Numberformat.Format = "@"; // Formato testo
                        }

                        // Aggiungi i dati
                        for (int row = 0; row < dataTable.Rows.Count; row++)
                        {
                            for (int col = 0; col < dataTable.Columns.Count; col++)
                            {
                                worksheet.Cells[row + 2, col + 1].Value = dataTable.Rows[row][col]?.ToString()?.Trim();
                            }
                        }

                        // Adatta automaticamente la larghezza delle colonne
                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                        // Salva il file Excel
                        await package.SaveAsAsync(new FileInfo(filePath));
                        _logger.LogInformation("File Excel salvato con successo: {FilePath}", filePath);
                    }

                    // Scrivi nel registro
                    var truncatedFilePath = truncatedPath.Length > 50 ? truncatedPath.Substring(0, 50) : truncatedPath;
                    var truncatedFileName = fileName.Length > 80 ? fileName.Substring(0, 80) : fileName;

                    _registroFileService.ScriviRegistroFile(
                        dataAMG: decimal.Parse(DateTime.Now.ToString("yyyyMMdd")),
                        oraHMS: DateTime.Now.ToString("HHmmss"),
                        dataAff: decimal.Parse(formData.DataAff ?? "0"),
                        daDataAff: decimal.Parse(formData.DaDataAff ?? "0"),
                        nazCor: richiesta.NazCor ?? string.Empty,
                        codCor: decimal.Parse(richiesta.CodCor ?? "0"),
                        codAcc: richiesta.CodAcc ?? string.Empty,
                        codUrg: richiesta.CodUrg ?? string.Empty,
                        totRic: richiesta.TotRic,
                        xPercorso: truncatedFilePath,
                        nomeFile: truncatedFileName,
                        formato: richiesta.Formato ?? string.Empty,
                        operatore: "ST8",
                        tenant: tenant
                    );

                    files.Add(new FileInfo(filePath));
                }

                _logger.LogInformation("Creazione dei file Excel completata. Numero di file creati: {FileCount}", files.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione dei file Excel.");
                throw;
            }

            return files;
        }
    }
}
