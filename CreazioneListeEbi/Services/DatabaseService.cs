using CreazioneListeEbi.Interfaces;
using CreazioneListeEbi.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace CreazioneListeEbi.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IConfiguration _configuration;

        public DatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<DataTable> GetDataAsync(FormData formData)
        {
            var dataTable = new DataTable();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                var query = @"SELECT PBSNCO, PBSCCO, KBARA1, PBSACC, PBSURG, PBSVALORE, TBIDEC, Count(*) AS TotAcc 
                              FROM PBSACOF0
                              INNER JOIN PBARDGF0 ON PBAANP = PBSAPR AND PBANUP = PBSNPR
                              INNER JOIN KBACORF0 ON KBANAZ = PBSNCO AND KBAPRG = PBSCCO AND KBATIPO = 'D'
                              INNER JOIN TBIACCF0 ON TBICAC = PBSACC
                              WHERE PBSDAF >= @DaDataAff AND PBSDAF <= @DataAff 
                              AND PBSPAC = '2' AND PBSSCARICO <> 'S' AND PBACOP <> 'SCO'
                              AND (PBACLI <> 16643 AND PBACLI <> 16644 AND PBACLI <> 16645 AND PBACLI <> 16646 
                              AND PBACLI <> 16698 AND PBACLI <> 16699 AND PBACLI <> 18011)
                              GROUP BY PBSNCO, PBSCCO, KBARA1, PBSACC, PBSURG, PBSVALORE, TBIDEC
                              ORDER BY PBSNCO, PBSCCO, PBSACC, PBSVALORE";

                if (!string.IsNullOrEmpty(formData.CodAcc))
                {
                    query += " AND PBSACC = @CodAcc";
                }
                if (!string.IsNullOrEmpty(formData.NumLotto))
                {
                    query += " AND PBALOTTO = @NumLotto";
                }

                using (var command = new SqlCommand(query, connection))
                {
                    // Aggiungi i parametri delle date variabili
                    var daDataAff = DateTime.ParseExact(formData.DaDataAff, "yyyyMMdd", null);
                    var dataAff = DateTime.ParseExact(formData.DataAff, "yyyyMMdd", null);

                    command.Parameters.AddWithValue("@DaDataAff", daDataAff.ToString("yyyyMMdd"));
                    command.Parameters.AddWithValue("@DataAff", dataAff.ToString("yyyyMMdd"));

                    // Aggiungi i parametri opzionali solo se presenti
                    if (!string.IsNullOrEmpty(formData.CodAcc))
                    {
                        command.Parameters.AddWithValue("@CodAcc", formData.CodAcc);
                    }
                    if (!string.IsNullOrEmpty(formData.NumLotto))
                    {
                        command.Parameters.AddWithValue("@NumLotto", Convert.ToInt32(formData.NumLotto));
                    }

                    StringBuilder finalQuery = new StringBuilder(query);
                    foreach (SqlParameter parameter in command.Parameters)
                    {
                        finalQuery.Replace(parameter.ParameterName, parameter.Value.ToString());
                    }

                    Console.WriteLine("Query Finale: ");
                    Console.WriteLine(finalQuery.ToString());

                    var adapter = new SqlDataAdapter(command);
                    await Task.Run(() => adapter.Fill(dataTable));
                }
            }

            return dataTable;
        }
    }
}
