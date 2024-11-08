using CreazioneListe.Interfaces;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.IO.Compression;

namespace CreazioneListe.Services
{
    public class FileService : IFileService
    {
        private readonly string _baseDirectory;
        private readonly IConfiguration _configuration;

        public FileService(IConfiguration configuration)
        {
            // Percorso base per salvare i file
            _baseDirectory = Path.Combine("C:\\Users\\Utente\\Desktop\\EXCEL");
            _configuration = configuration;
        }

        public List<FileInfo> GetFilesList(string tenant)
        {
            var directoryPath = Path.Combine(_baseDirectory, tenant, DateTime.Now.ToString("yyyyMMdd"));

            if (!Directory.Exists(directoryPath))
            {
                // Se la directory non esiste, restituisce una lista vuota
                return new List<FileInfo>();
            }

            // Recupera tutti i file nella directory specificata
            return Directory.GetFiles(directoryPath)
                            .Select(filePath => new FileInfo(filePath))
                            .ToList();
        }

        public FileInfo GetFile(string tenant, string fileName)
        {
            var directoryPath = Path.Combine(_baseDirectory, tenant, DateTime.Now.ToString("yyyyMMdd"));
            var filePath = Path.Combine(directoryPath, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Il file richiesto non è stato trovato.", fileName);
            }

            return new FileInfo(filePath);
        }

        public FileInfo CreateZipFile(string tenant)
        {
            var directoryPath = Path.Combine(_baseDirectory, tenant, DateTime.Now.ToString("yyyyMMdd"));

            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException("Directory non trovata.");
            }

            var files = Directory.GetFiles(directoryPath);
            if (files.Length == 0)
            {
                throw new FileNotFoundException("Nessun file disponibile per il download.");
            }

            var zipFilePath = Path.Combine(directoryPath, $"TuttiFile_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                }
            }

            return new FileInfo(zipFilePath);
        }
    }
}
