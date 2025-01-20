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
                    PBSNCO = row.Table.Columns.Contains("PBSNCO") ? row["PBSNCO"].ToString()?.Trim() : null,
                    PBSCCO = row.Table.Columns.Contains("PBSCCO") ? row["PBSCCO"].ToString()?.Trim() : null,
                    KBARA1 = row.Table.Columns.Contains("KBARA1") ? row["KBARA1"].ToString()?.Trim() : null,
                    PBSACC = row.Table.Columns.Contains("PBSACC") ? row["PBSACC"].ToString()?.Trim() : null,
                    PBSURG = row.Table.Columns.Contains("PBSURG") ? row["PBSURG"].ToString()?.Trim() : null,
                    PBSVALORE = row.Table.Columns.Contains("PBSVALORE") ? row["PBSVALORE"].ToString()?.Trim() : null,
                    TBIDEC = row.Table.Columns.Contains("TBIDEC") ? row["TBIDEC"].ToString()?.Trim() : null,
                    TotAcc = row.Table.Columns.Contains("TotAcc") ? row["TotAcc"].ToString()?.Trim() : null
                };
                list.Add(item);
            }

            return list;
        }
    }
}
