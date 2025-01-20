using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;

namespace CreazioneListe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SelectedQueryApiController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly IColonneFiltraggioService _colonneFiltraggioService;
        private readonly ILogger<SelectedQueryApiController> _logger;
        private readonly IExcelService _excelService;

        public SelectedQueryApiController(IDatabaseService databaseService, IColonneFiltraggioService colonneFiltraggioService, ILogger<SelectedQueryApiController> logger, IExcelService excelService)
        {
            _databaseService = databaseService;
            _colonneFiltraggioService = colonneFiltraggioService;
            _logger = logger;
            _excelService = excelService;
        }

        [HttpGet("GetSelectedDataToExcel")]
        [SwaggerOperation(Summary = "Recupera dati selezionati e genera un file Excel.", Description = "Restituisce un file Excel con Protocollo, Codice Fiscale e Cliente se richiesto.")]
        [SwaggerResponse(200, "File Excel generato con successo", typeof(FileResult))]
        [SwaggerResponse(400, "Parametri non validi")]
        [SwaggerResponse(500, "Errore interno del server")]
        public async Task<IActionResult> GetSelectedDataToExcel(
            [FromQuery] string daDataAff,
            [FromQuery] string dataAff,
            [FromQuery] string nazCor,
            [FromQuery] string codCor,
            [FromQuery] string codAcc,
            [FromQuery] string codUrg,
            [FromQuery] int totRic,
            [FromQuery] string formato,
            [FromQuery] string tenant)
        {
            if (string.IsNullOrWhiteSpace(daDataAff) || string.IsNullOrWhiteSpace(dataAff))
            {
                return BadRequest("I parametri DaDataAff e DataAff sono obbligatori.");
            }

            try
            {
                var richiestaExcel = new RichiestaExcel
                {
                    DaDataAff = daDataAff,
                    DataAff = dataAff,
                    NazCor = nazCor,
                    CodCor = codCor,
                    CodAcc = codAcc,
                    CodUrg = codUrg,
                    TotRic = totRic,
                    Formato = formato
                };

                var formData = new FormData
                {
                    DaDataAff = daDataAff,
                    DataAff = dataAff,
                    NazCor = nazCor,
                    CodCor = codCor,
                    CodAcc = codAcc
                };

                _logger.LogInformation("Esecuzione query GetSelectedAsync per tenant {Tenant}.", tenant);
                var result = await _databaseService.GetSelectedAsync(richiestaExcel, formData, tenant);
                var filteredResult = _colonneFiltraggioService.FiltraColonne(result, richiestaExcel, tenant);

                var dataTables = new List<DataTable> { filteredResult };
                var generatedFiles = await _excelService.CreateExcelFilesAsync(dataTables, new List<RichiestaExcel> { richiestaExcel }, formData, tenant);

                var file = generatedFiles.FirstOrDefault();
                if (file == null || !System.IO.File.Exists(file.FullName))
                {
                    return StatusCode(500, "Errore nella generazione del file Excel.");
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(file.FullName, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei dati selezionati e la generazione del file Excel.");
                return StatusCode(500, "Errore interno del server. Si prega di riprovare più tardi.");
            }
        }
    }
}
