using CreazioneListe.Models;
using System.Data;

namespace CreazioneListe.Interfaces
{
    public interface IDatabaseService
    {
        Task<DataTable> GetSelectAsync(FormData formData);
        Task<DataTable> GetSelectedAsync(RichiestaExcel richiestaExcel, FormData formData);
    }
}
