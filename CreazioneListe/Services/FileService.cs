using CreazioneListe.Interfaces;
using System.IO.Compression;

namespace CreazioneListe.Services
{
    public class FileService : IFileService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;

        private readonly string _ebiBaseDirectory;
        private readonly string _sscBaseDirectory;

        public FileService(IConfiguration configuration, ILogger<FileService> logger)
        {
            _ebiBaseDirectory = @"\\10.10.20.5\f\Domains\AdcExe\Corrisp\FilesFornitori";
            _sscBaseDirectory = @"\\10.10.12.5\f\Domains\AdcExe\Corrisp\FilesFornitori";
            _configuration = configuration;
            _logger = logger;
        }

        private string GetBaseDirectory(string tenant)
        {
            return tenant.ToUpper() switch
            {
                "EBI" => _ebiBaseDirectory,
                "SSC" => _sscBaseDirectory,
                _ => throw new ArgumentException($"Tenant non riconosciuto: {tenant}")
            };
        }

        public List<FileInfo> GetFilesList(string tenant)
        {
            var baseDirectory = GetBaseDirectory(tenant);
            var directoryPath = Path.Combine(baseDirectory, DateTime.Now.ToString("yyyyMMdd"));
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
            var baseDirectory = GetBaseDirectory(tenant);
            var directoryPath = Path.Combine(baseDirectory, DateTime.Now.ToString("yyyyMMdd"));
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
            var baseDirectory = GetBaseDirectory(tenant);
            var directoryPath = Path.Combine(baseDirectory, DateTime.Now.ToString("yyyyMMdd"));
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
        public List<FileInfo> GetFilesByDataAff(string tenant, string dataAff)
        {
            var baseDirectory = GetBaseDirectory(tenant);
            var directoryPath = Path.Combine(baseDirectory, dataAff);
            _logger.LogInformation("Recupero della lista di file dalla directory: {DirectoryPath}", directoryPath);

            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning("Directory non trovata: {DirectoryPath}", directoryPath);
                return new List<FileInfo>();
            }

            var files = Directory.GetFiles(directoryPath)
                                 .Select(filePath => new FileInfo(filePath))
                                 .ToList();

            _logger.LogInformation("Numero di file trovati: {FileCount} per la data {DataAff}", files.Count, dataAff);
            return files;
        }
        public FileInfo GetFileByDataAff(string tenant, string dataAff, string fileName)
        {
            var baseDirectory = GetBaseDirectory(tenant);
            var directoryPath = Path.Combine(baseDirectory, dataAff);
            var filePath = Path.Combine(directoryPath, fileName);

            _logger.LogInformation("Recupero del file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("Il file richiesto non è stato trovato: {FileName}", fileName);
                throw new FileNotFoundException("Il file richiesto non è stato trovato.", fileName);
            }

            return new FileInfo(filePath);
        }
        public FileInfo CreateZipFileByDataAff(string tenant, string dataAff)
        {
            var baseDirectory = GetBaseDirectory(tenant);
            var directoryPath = Path.Combine(baseDirectory, dataAff);
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

            var zipFilePath = Path.Combine(directoryPath, $"TuttiFile_{dataAff}_{DateTime.Now:HHmmss}.zip");
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
