using CreazioneListe.Models;
using System.Data;

namespace CreazioneListe.Interfaces
{
    public interface IExcelService
    {
        Task<List<FileInfo>> CreateExcelFilesAsync(List<DataTable> dataTables, List<RichiestaExcel> richiesteExcel, string tenant);
    }
}
