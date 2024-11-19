namespace CreazioneListe.Interfaces
{
    public interface IFileService
    {
        List<FileInfo> GetFilesList(string tenant);
        FileInfo GetFile(string tenant, string fileName);
        FileInfo CreateZipFile(string tenant);
    }
}
