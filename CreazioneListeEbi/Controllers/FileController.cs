using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace CreazioneListe.Controllers
{
    public class FileController : Controller
    {
        private readonly string _filesDirectory;
        public FileController()
        {
            // Percorso in cui vengono salvati i file creati (wwwroot/files).
            _filesDirectory = Path.Combine("C:\\Users\\Utente\\Desktop\\EXCEL", DateTime.Now.ToString("yyyyMMdd"));
        }

        public IActionResult ListaFile()
        {
            if (!Directory.Exists(_filesDirectory))
            {
                // Se la directory non esiste, la crea.
                Directory.CreateDirectory(_filesDirectory);
            }

            // Recupera tutti i file nella directory specificata.
            var files = Directory.GetFiles(_filesDirectory)
                                 .Select(filePath => new FileInfo(filePath))
                                 .ToList();

            return View(files);
        }

        public IActionResult Download(string fileName)
        {
            // Combina il nome del file con la directory per ottenere il percorso completo.
            var filePath = Path.Combine(_filesDirectory, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                // Se il file non esiste, restituisce una vista di errore.
                return NotFound("Il file richiesto non è stato trovato.");
            }

            // Restituisce il file come FileStreamResult per il download.
            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                stream.CopyTo(memory);
            }
            memory.Position = 0;

            return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public IActionResult DownloadAll()
        {
            if (!Directory.Exists(_filesDirectory))
            {
                return NotFound("Directory non trovata.");
            }

            var files = Directory.GetFiles(_filesDirectory);
            if (files.Length == 0)
            {
                return NotFound("Nessun file disponibile per il download.");
            }

            var zipFilePath = Path.Combine(_filesDirectory, $"TuttiFile_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                }
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
            {
                stream.CopyTo(memory);
            }
            memory.Position = 0;

            return File(memory, "application/zip", Path.GetFileName(zipFilePath));
        }
    }
}