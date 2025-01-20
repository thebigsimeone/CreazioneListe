using CreazioneListe.Interfaces;
using System.IO.Compression;

namespace CreazioneListe.Services
{
    public class FileService : IFileService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;

        public FileService(IConfiguration configuration, ILogger<FileService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private string GetBaseDirectory(string tenant)
        {
            var baseDirectory = _configuration[$"Paths:{tenant.ToUpper()}"];
            if (string.IsNullOrEmpty(baseDirectory))
                throw new ArgumentException($"Percorso non configurato per il tenant: {tenant}");
            return baseDirectory;
        }

        public List<FileInfo> GetFilesList(string tenant)
        {
            var directoryPath = Path.Combine(GetBaseDirectory(tenant), DateTime.Now.ToString("yyyyMMdd"));
            _logger.LogInformation("Recupero dei file dalla directory: {DirectoryPath}", directoryPath);

            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Directory non trovata: {DirectoryPath}", directoryPath);
                return new List<FileInfo>();
            }

            return Directory.GetFiles(directoryPath)
                            .Select(filePath => new FileInfo(filePath))
                            .ToList();
        }

        public FileInfo GetFile(string tenant, string fileName)
        {
            var directoryPath = Path.Combine(GetBaseDirectory(tenant), DateTime.Now.ToString("yyyyMMdd"));
            var filePath = Path.Combine(directoryPath, fileName);

            _logger.LogInformation("Recupero del file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("Il file non è stato trovato: {FileName}", fileName);
                throw new FileNotFoundException("File non trovato.", fileName);
            }

            return new FileInfo(filePath);
        }

        public FileInfo CreateZipFile(string tenant)
        {
            var directoryPath = Path.Combine(GetBaseDirectory(tenant), DateTime.Now.ToString("yyyyMMdd"));
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogError("Directory non trovata: {DirectoryPath}", directoryPath);
                throw new DirectoryNotFoundException("Directory non trovata.");
            }

            var files = Directory.GetFiles(directoryPath);
            if (!files.Any())
            {
                _logger.LogWarning("Nessun file disponibile per il download nella directory: {DirectoryPath}", directoryPath);
                throw new FileNotFoundException("Nessun file disponibile.");
            }

            var zipFilePath = Path.Combine(directoryPath, $"TuttiFile_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
            try
            {
                using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    foreach (var file in files)
                    {
                        zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
                }
                _logger.LogInformation("ZIP creato: {ZipFilePath}", zipFilePath);
                return new FileInfo(zipFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella creazione dello ZIP: {ZipFilePath}", zipFilePath);
                throw;
            }
        }

        public List<FileInfo> GetFilesByDataAff(string tenant, string dataAff)
        {
            var directoryPath = Path.Combine(GetBaseDirectory(tenant), dataAff);
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Directory non trovata: {DirectoryPath}", directoryPath);
                return new List<FileInfo>();
            }

            return Directory.GetFiles(directoryPath)
                            .Select(filePath => new FileInfo(filePath))
                            .ToList();
        }

        public FileInfo GetFileByDataAff(string tenant, string dataAff, string fileName)
        {
            var directoryPath = Path.Combine(GetBaseDirectory(tenant), dataAff);
            var filePath = Path.Combine(directoryPath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogError("File non trovato: {FilePath}", filePath);
                throw new FileNotFoundException("File non trovato.", fileName);
            }

            return new FileInfo(filePath);
        }

        public FileInfo CreateZipFileByDataAff(string tenant, string dataAff)
        {
            var directoryPath = Path.Combine(GetBaseDirectory(tenant), dataAff);
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException("Directory non trovata.");

            var files = Directory.GetFiles(directoryPath);
            if (!files.Any())
                throw new FileNotFoundException("Nessun file disponibile.");

            var zipFilePath = Path.Combine(directoryPath, $"TuttiFile_{dataAff}_{DateTime.Now:HHmmss}.zip");
            try
            {
                using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    foreach (var file in files)
                    {
                        zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
                }
                return new FileInfo(zipFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione dello ZIP: {ZipFilePath}", zipFilePath);
                throw;
            }
        }
    }
}
