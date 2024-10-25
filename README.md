## Azure SQL AUTH

I already have a azure sql database running from another project. I registered my application and used the service principle to execute queries on the database.
Used both methods `certificates`  and `client secrets`.

## Client Secrets 
Client secret was created in the app registration. Currently use it as an environment variable running locally, but would use Azure Key Vault if it's an app I'll deploy.

``` csharp
        public static async Task GetMuseumObjectsClientSecret()
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

        }
```

## Certificates 
Create and openssl certificate and upload the public certificate as part of the app registration. So it could decrypt the token we send and confirm we're holders of the private key.
Pass on the token we're given so we  could use to access the resource.

```csharp
        public static async Task GetMuseumObjectsCertificate()
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

        }


```

