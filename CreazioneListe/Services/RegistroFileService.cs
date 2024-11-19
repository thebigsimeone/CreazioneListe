namespace CreazioneListe.Services
{
    using CreazioneListe.Interfaces;
    using Microsoft.Data.SqlClient;
    using System.Data;

    public class RegistroFileService : IRegistroFileService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegistroFileService> _logger;

        public RegistroFileService(IConfiguration configuration, ILogger<RegistroFileService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void ScriviRegistroFile(decimal dataAMG, string oraHMS, decimal dataAff, decimal daDataAff, string nazCor, string codCor, string codAcc, string codUrg, int totRic, string xPercorso, string nomeFile, string formato, string operatore, string tenant)
        {
            string connectionString = tenant == "EBI"
                ? _configuration.GetConnectionString("DefaultConnection_EBI")
                : _configuration.GetConnectionString("DefaultConnection_SSC");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    _logger.LogInformation($"Connessione al database aperta con successo per ScriviRegistroFile per tenant {tenant}.");

                    // Query per selezionare il record con ID_Reg = 999999999
                    string selectQuery = "SELECT * FROM RegistroFile WHERE ID_Reg = 999999999";

                    using (var command = new SqlCommand(selectQuery, connection))
                    using (var adapter = new SqlDataAdapter(command))
                    using (var builder = new SqlCommandBuilder(adapter))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        // Se non esiste un record, ne aggiungiamo uno nuovo
                        if (dataTable.Rows.Count == 0)
                        {
                            DataRow newRow = dataTable.NewRow();
                            newRow["ID_DataReg"] = dataAMG;
                            newRow["ID_Ora"] = oraHMS;
                            newRow["ID_Daf"] = dataAff;
                            newRow["ID_Daf1"] = daDataAff;
                            newRow["ID_Operatore"] = operatore;
                            newRow["ID_NazCor"] = nazCor;
                            newRow["ID_CodCor"] = codCor;
                            newRow["ID_Acc"] = codAcc;
                            newRow["ID_Urg"] = codUrg;
                            newRow["ID_NumRic"] = totRic;
                            newRow["IF_Formato"] = formato;
                            newRow["ID_Path"] = xPercorso;
                            newRow["ID_File"] = nomeFile;

                            dataTable.Rows.Add(newRow);
                            adapter.Update(dataTable);

                            _logger.LogInformation($"Nuovo record aggiunto con successo al RegistroFile per tenant {tenant}.");
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, $"Errore SQL durante l'esecuzione di ScriviRegistroFile per tenant {tenant}.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore generico durante l'esecuzione di ScriviRegistroFile per tenant {tenant}.");
                throw;
            }
        }
    }
}
