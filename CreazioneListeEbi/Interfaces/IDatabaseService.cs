using CreazioneListeEbi.Models;
using System.Data;

namespace CreazioneListeEbi.Interfaces
{
    public interface IDatabaseService
    {
        Task<DataTable> GetDataAsync(FormData formData);
    }
}
