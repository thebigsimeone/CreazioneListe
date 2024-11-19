using CreazioneListe.Interfaces;
using System.IO.Compression;

namespace CreazioneListe.Services
{
    public class FileService : IFileService
    {
        private readonly string _baseDirectory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;

        public FileService(IConfiguration configuration, ILogger<FileService> logger)
        {
            _baseDirectory = Path.Combine("C:\\Users\\Utente\\Desktop\\EXCEL");
            _configuration = configuration;
            _logger = logger;
        }

        public List<FileInfo> GetFilesList(string tenant)
        {
            var directoryPath = Path.Combine(_baseDirectory, tenant, DateTime.Now.ToString("yyyyMMdd"));
            _logger.LogInformation("Recupero della lista di file dalla directory: {DirectoryPath}", directoryPath);

            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Directory non trovata: {DirectoryPath}", directoryPath);
                return new List<FileInfo>();
            }

            var files = Directory.GetFiles(directoryPath)
                                 .Select(filePath => new FileInfo(filePath))
                                 .ToList();

            _logger.LogInformation("Numero di file trovati: {FileCount}", files.Count);
            return files;
        }

        public FileInfo GetFile(string tenant, string fileName)
        {
            var directoryPath = Path.Combine(_baseDirectory, tenant, DateTime.Now.ToString("yyyyMMdd"));
            var filePath = Path.Combine(directoryPath, fileName);

            _logger.LogInformation("Recupero del file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("Il file richiesto non è stato trovato: {FileName}", fileName);
                throw new FileNotFoundException("Il file richiesto non è stato trovato.", fileName);
            }

            return new FileInfo(filePath);
        }

        public FileInfo CreateZipFile(string tenant)
        {
            var directoryPath = Path.Combine(_baseDirectory, tenant, DateTime.Now.ToString("yyyyMMdd"));
            _logger.LogInformation("Creazione di un archivio ZIP dalla directory: {DirectoryPath}", directoryPath);

            if (!Directory.Exists(directoryPath))
            {
                _logger.LogError("Directory non trovata: {DirectoryPath}", directoryPath);
                throw new DirectoryNotFoundException("Directory non trovata.");
            }

            var files = Directory.GetFiles(directoryPath);
            if (files.Length == 0)
            {
                _logger.LogWarning("Nessun file disponibile per il download nella directory: {DirectoryPath}", directoryPath);
                throw new FileNotFoundException("Nessun file disponibile per il download.");
            }

            var zipFilePath = Path.Combine(directoryPath, $"TuttiFile_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
            _logger.LogInformation("Creazione del file ZIP: {ZipFilePath}", zipFilePath);

            try
            {
                using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    foreach (var file in files)
                    {
                        _logger.LogInformation("Aggiunta del file {File} all'archivio ZIP", file);
                        zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
                }

                _logger.LogInformation("File ZIP creato con successo: {ZipFilePath}", zipFilePath);
                return new FileInfo(zipFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione del file ZIP: {ZipFilePath}", zipFilePath);
                throw;
            }
        }
    }
}
