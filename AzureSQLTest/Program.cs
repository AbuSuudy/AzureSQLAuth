using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace AzureSQLTest
{
    internal class Program
    {
        private static List<MuseumObject> musuemObjects = new List<MuseumObject>();

        public static async Task<List<MuseumObject>> GetMuseumObjectsClientSecret()
        {
            var appID = Environment.GetEnvironmentVariable("APP_ID");
            var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");

            string connectionString = @"Server=theariseensqlserver.database.windows.net;"
                  + "Authentication=Active Directory Service Principal; Encrypt=True;"
                  + $"Database=TheArtiseenDB; User Id={appID}; Password={clientSecret}";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                await MuseumItemsDatabaseCall(connection, "SELECT TOP (10) * FROM [dbo].[DailyMuseumItems]");

            };

            return musuemObjects;
        }


        public static async Task<List<MuseumObject>> GetMuseumObjectsCertificate()
        {

            var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            var certPath = Environment.GetEnvironmentVariable("CERTIFICATE_PATH");
            var certPassword = Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD");

            string authority = $"https://login.microsoftonline.com/{tenantId}/";

            string[] scopes = new string[] { "https://database.windows.net/.default" };

            var clientSecretCredential = new ClientCertificateCredential(tenantId, clientId, certPath);

            X509Certificate2 certificate = new X509Certificate2(certPath, certPassword);

            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithCertificate(certificate)
                .WithAuthority(new Uri(authority))
            .Build();

            AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            using (var connection = new SqlConnection("Server=tcp:theariseensqlserver.database.windows.net,1433; Database=TheArtiseenDB;"))
            {
                connection.AccessToken = result.AccessToken;

                await connection.OpenAsync();

                await MuseumItemsDatabaseCall(connection, "SELECT * FROM [dbo].[DailyMuseumItems] Order by Id OFFSET 10 ROW FETCH NEXT 10 ROWS ONLY;");

            }

            return musuemObjects;

        }

        public static async Task MuseumItemsDatabaseCall(SqlConnection connection, string sqlScript)
        {
            var command = new SqlCommand(sqlScript, connection);
            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        musuemObjects.Add(new MuseumObject
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            PrimaryImageURL = reader.GetString(3),
                            ObjectURL = reader.GetString(5),
                            Culture = reader.GetString(6),
                            Artist = reader.GetString(10),
                            ExternalId = reader.GetInt32(11)

                        });
                    }
                }
            }
        }

        static async Task Main(string[] args)
        {
            try
            {
                await GetMuseumObjectsClientSecret();
                await GetMuseumObjectsCertificate();

                Console.WriteLine($"Size: {musuemObjects.Count}");

                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.WriteLine(JsonSerializer.Serialize(musuemObjects, options));

            }
            catch (Exception ex)
            {
            }
        }
    }
}
