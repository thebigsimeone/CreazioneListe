using CreazioneListe.Models;
using System.Data;

namespace CreazioneListe.Interfaces
{
    public interface IColonneFiltraggioService
    {
        DataTable FiltraColonne(DataTable dataTable, RichiestaExcel richiesta, string tenant);
    }
}
