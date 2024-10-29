using CreazioneListeEbi.Models;
using System.Data;

namespace CreazioneListeEbi.Interfaces
{
    public interface IExcelService
    {
        Task<string> CreateExcelFileAsync(RichiestaExcel richiesta);
        Task<bool> CreateDirectoryIfNotExistAsync(string path);
    }
}
