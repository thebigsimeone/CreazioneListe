using Renci.SshNet;

namespace CreazioneListe.Services
{
    public class SftpService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        public SftpService(string host, int port, string username, string password)
        {
            _host = host;
            _port = port;
            _username = username;
            _password = password;
        }
        public List<string> ListDirectories(string remotePath)
        {
            using (var client = new SftpClient(_host, _port, _username, _password))
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
            using (var client = new SftpClient(_host, _port, _username, _password))
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
            using (var client = new SftpClient(_host, _port, _username, _password))
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
