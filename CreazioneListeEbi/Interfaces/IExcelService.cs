using CreazioneListe.Models;
using System.Data;

namespace CreazioneListe.Interfaces
{
    public interface IExcelService
    {
        Task<string> CreateExcelFileAsync(RichiestaExcel richiesta);
        Task<bool> CreateDirectoryIfNotExistAsync(string path);
    }
}
