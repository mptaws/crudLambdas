using System;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using MongoDB.Driver;

namespace dnCrud
{
    public class MongoConnect
    {
        public static string GetSecret()
        {
            string secretName = Environment.GetEnvironmentVariable("SECRETNAME");
            string region = Environment.GetEnvironmentVariable("REGION");
            string secret = "";

            MemoryStream memoryStream = new MemoryStream();

            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            GetSecretValueRequest request = new GetSecretValueRequest();
            request.SecretId = secretName;
            request.VersionStage = "AWSCURRENT"; // VersionStage defaults to AWSCURRENT if unspecified.

            GetSecretValueResponse response = null;

            try
            {
                response = client.GetSecretValueAsync(request).Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            if (response.SecretString != null)
            {
                secret = response.SecretString;
            }
            else
            {
                memoryStream = response.SecretBinary;
                StreamReader reader = new StreamReader(memoryStream);
                string decodedBinarySecret = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
                secret = decodedBinarySecret;
            }

            return secret;

        }

        public class SecretData
        {
            public string username { get; set; }
            public string password { get; set; }
            public string engine { get; set; }
            public string host { get; set; }
            public int port { get; set; }
            public bool ssl { get; set; }
            public string identifier { get; set; }
        }

        public static MongoClient Connect()
        {
            var secretResult = GetSecret();

            SecretData fromSecretsManager = JsonSerializer.Deserialize<SecretData>(secretResult);

            string template = "mongodb://{0}:{1}@{2}:{3}/?ssl=true&replicaSet=rs0&readPreference={4}&retryWrites=false";
            string username = fromSecretsManager.username;
            string password = fromSecretsManager.password;
            string readPreference = "secondaryPreferred";
            string clusterEndpoint = fromSecretsManager.host;
            string port = fromSecretsManager.port.ToString();
            string connectionString = String.Format(template, username, password, clusterEndpoint, port, readPreference);

            var pathtoCAFile = "/opt/rds-combined-ca-bundle.pem";

            X509Store localTrustStore = new X509Store(StoreName.Root);
            X509Certificate2Collection certificateCollection = new X509Certificate2Collection();
            certificateCollection.Import(pathtoCAFile);
            try
            {
                localTrustStore.Open(OpenFlags.ReadWrite);
                localTrustStore.AddRange(certificateCollection);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Root certificate import failed: " + ex.Message);
                throw;
            }
            finally
            {
                localTrustStore.Close();
            }

            var settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            var client = new MongoClient(settings);

            return client;
        }

    }
}