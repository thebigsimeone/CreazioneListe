using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;

namespace CreazioneListe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SelectQueryApiController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<SelectQueryApiController> _logger;

        public SelectQueryApiController(IDatabaseService databaseService, ILogger<SelectQueryApiController> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        [HttpGet("GetData")]
        [SwaggerOperation(Summary = "Recupera i dati filtrati.", Description = "Restituisce i risultati della query basata sui parametri forniti.")]
        [SwaggerResponse(200, "Dati recuperati con successo", typeof(IEnumerable<FormData>))]
        [SwaggerResponse(400, "Parametri non validi")]
        [SwaggerResponse(500, "Errore interno del server")]
        public async Task<IActionResult> GetSelectList([FromQuery] FormData formData, [FromQuery] string tenant)
        {
            if (string.IsNullOrWhiteSpace(formData.DaDataAff) || string.IsNullOrWhiteSpace(formData.DataAff))
            {
                return BadRequest("I parametri DaDataAff e DataAff sono obbligatori.");
            }

            try
            {
                _logger.LogInformation("Esecuzione query con DaDataAff: {DaDataAff}, DataAff: {DataAff}, Tenant: {Tenant}", formData.DaDataAff, formData.DataAff, tenant);

                var result = await _databaseService.GetSelectAsync(formData, tenant);
                var listResult = DataTableToList(result);
                return Ok(listResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei dati.");
                return StatusCode(500, "Errore interno del server. Si prega di riprovare più tardi.");
            }
        }

        private List<object> DataTableToList(DataTable dataTable)
        {
            var list = new List<object>();

            foreach (DataRow row in dataTable.Rows)
            {
                var item = new
                {
                    FornitoreNazione = row.Table.Columns.Contains("FornitoreNazione") ? row["FornitoreNazione"].ToString()?.Trim() : null,
                    FornitoreCodice = row.Table.Columns.Contains("FornitoreCodice") ? row["FornitoreCodice"].ToString()?.Trim() : null,
                    RagioneSocialeFornitore = row.Table.Columns.Contains("RagioneSocialeFornitore") ? row["RagioneSocialeFornitore"].ToString()?.Trim() : null,
                    AccertamentoCodice = row.Table.Columns.Contains("AccertamentoCodice") ? row["AccertamentoCodice"].ToString()?.Trim() : null,
                    UrgenzaCodice = row.Table.Columns.Contains("UrgenzaCodice") ? row["UrgenzaCodice"].ToString()?.Trim() : null,
                    Importo = row.Table.Columns.Contains("Importo") ? row["Importo"].ToString()?.Trim() : null,
                    DescrizioneAccertamento = row.Table.Columns.Contains("DescrizioneAccertamento") ? row["DescrizioneAccertamento"].ToString()?.Trim() : null,
                    TotAcc = row.Table.Columns.Contains("TotAcc") ? row["TotAcc"].ToString()?.Trim() : null
                };
                list.Add(item);
            }

            return list;
        }
    }
}

