using Renci.SshNet;

namespace CreazioneListe.Services
{
    public class SftpService
    {
        private readonly IConfiguration _configuration;

        public SftpService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SftpClient CreateClient() => new SftpClient(
            Required(_configuration, "Sftp:Host"),
            _configuration.GetValue<int>("Sftp:Port", 22),
            Required(_configuration, "Sftp:Username"),
            Required(_configuration, "Sftp:Password"));

        private static string Required(IConfiguration configuration, string key) =>
            !string.IsNullOrWhiteSpace(configuration[key]) ? configuration[key]!
                : throw new InvalidOperationException($"Configurare {key} tramite configurazione privata.");

        public void UploadFile(Stream stream, string directory, string fileName)
        {
            using var client = CreateClient();
            client.Connect();
            if (!client.Exists(directory)) client.CreateDirectory(directory);
            client.UploadFile(stream, $"{directory.TrimEnd('/')}/{fileName}");
            client.Disconnect();
        }
        public List<string> ListDirectories(string remotePath)
        {
            using (var client = CreateClient())
            {
                client.Connect();
                var directories = new List<string>();
                foreach (var item in client.ListDirectory(remotePath))
                {
                    if (item.IsDirectory && item.Name != "." && item.Name != "..")
                    {
                        directories.Add(item.FullName);
                    }
                }
                client.Disconnect();
                return directories;
            }
        }

        public List<string> ListFiles(string remotePath)
        {
            using (var client = CreateClient())
            {
                client.Connect();
                var files = new List<string>();
                foreach (var item in client.ListDirectory(remotePath))
                {
                    if (!item.IsDirectory)
                    {
                        files.Add(item.FullName);
                    }
                }
                client.Disconnect();
                return files;
            }
        }

        public Stream DownloadFile(string remoteFilePath)
        {
            using (var client = CreateClient())
            {
                client.Connect();
                var memoryStream = new MemoryStream();
                client.DownloadFile(remoteFilePath, memoryStream);
                memoryStream.Position = 0;
                client.Disconnect();
                return memoryStream;
            }
        }
    }
}
