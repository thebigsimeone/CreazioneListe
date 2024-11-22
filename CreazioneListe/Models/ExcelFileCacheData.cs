using System.Data;

namespace CreazioneListe.Models
{
    public class ExcelFileCacheData
    {
        public List<DataTable> DataTables { get; set; }
        public List<RichiestaExcel> RichiesteExcel { get; set; }
        public FormData FormData { get; set; }
    }

}
