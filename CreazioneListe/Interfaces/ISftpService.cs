namespace CreazioneListe.Interfaces
{
    public interface ISftpService
    {
        List<string> ListDirectories(string remotePath);
        List<string> ListFiles(string remotePath);
        Stream DownloadFile(string remoteFilePath);
    }
}
