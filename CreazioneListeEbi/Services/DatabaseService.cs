using Azure.Messaging;
using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace CreazioneListe.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IConfiguration _configuration;

        public DatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<DataTable> GetSelectAsync(FormData formData)
        {
            var dataTable = new DataTable();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection_EBI")))
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

                    Console.WriteLine("Query Table Select: ");
                    Console.WriteLine(finalQuery.ToString());

                    var adapter = new SqlDataAdapter(command);
                    await Task.Run(() => adapter.Fill(dataTable));
                }
            }

            return dataTable;
        }

        public async Task<DataTable> GetSelectedAsync(RichiestaExcel richiestaExcel, FormData formData)
        {
            var dataTable = new DataTable();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection_EBI")))
            {
                var query = @"SELECT isNull(T1.TBBDES,'') as TipoIND, isNull(T2.TBBDES,'') as TipoCONT, PBARDGF0.*, IBAOGGF0.*, DATORILAV.*
                      FROM PBSACOF0
                      INNER JOIN PBARDGF0 ON PBAANP = PBSAPR AND PBANUP = PBSNPR
                      LEFT JOIN IBAOGGF0 ON IBACOG = PBAOGG
                      LEFT JOIN TBBTABF0 as T1 ON T1.TBBTTA = 'IND' and T1.TBBCLI = 'IT' And T1.TBBCTA = IBAIND
                      LEFT JOIN DATORILAV ON LAVOGG = PBAOGG
                      LEFT JOIN TBBTABF0 as T2 ON T2.TBBTTA = 'DCO' and T2.TBBCLI = 'IT' And T2.TBBCTA = LAVCONT
                      WHERE PBSDAF >= @DaDataAff And PBSDAF <= @DataAff And PBSPAC = '2' And PBSSCARICO <> 'S'
                      And PBSNCO = @NazCor And PBSCCO = @CodCor And PBSACC = @CodAcc And PBSURG = @CodUrg And PBACOP <> 'SCO'";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DaDataAff", Convert.ToInt32(formData.DaDataAff));
                    command.Parameters.AddWithValue("@DataAff", Convert.ToInt32(formData.DataAff));
                    command.Parameters.AddWithValue("@NazCor", formData.NazCor ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CodCor", Convert.ToInt32(formData.CodCor));
                    command.Parameters.AddWithValue("@CodAcc", formData.CodAcc ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CodUrg", richiestaExcel.CodUrg ?? (object)DBNull.Value);

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
