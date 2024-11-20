namespace CreazioneListe.Interfaces
{
    public interface IFileService
    {
        List<FileInfo> GetFilesList(string tenant);
        FileInfo GetFile(string tenant, string fileName);
        FileInfo CreateZipFile(string tenant);
        List<FileInfo> GetFilesByDataAff(string tenant, string dataAff);
        FileInfo GetFileByDataAff(string tenant, string dataAff, string fileName);
        FileInfo CreateZipFileByDataAff(string tenant, string dataAff);
    }
}
