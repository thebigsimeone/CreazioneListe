using CreazioneListeEbi.Interfaces;
using CreazioneListeEbi.Models;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;

namespace CreazioneListeEbi.Services
{
    public class ExcelService : IExcelService
    {
        private readonly IConfiguration _configuration;

        public ExcelService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> CreateExcelFileAsync(RichiestaExcel richiesta)
        {
            var directoryPath = Path.Combine("FilesFornitori", DateTime.Now.ToString("yyyyMMdd"));
            await CreateDirectoryIfNotExistAsync(directoryPath);

            var fileName = $"{richiesta.NazCor}-{richiesta.CodCor}_{richiesta.TotRic}_{richiesta.CodAcc}_{richiesta.CodUrg}_EBI_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var filePath = Path.Combine(directoryPath, fileName);

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets.Add("Dati");
                // Popolare il worksheet con i dati
                worksheet.Cells[1, 1].Value = "Esempio"; // Placeholder, popolare con dati effettivi

                // Salva il file
                await package.SaveAsync();
            }

            return filePath;
        }

        public async Task<bool> CreateDirectoryIfNotExistAsync(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                await Task.CompletedTask; // Solo per simulare un metodo asincrono
            }
            return true;
        }
    }
}
