using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

Console.WriteLine("AZURE WORKLOAD IDENITY IN AKS");

DefaultAzureCredentialOptions options = new();

var credentials = GetCredential(options);

var sec = await FetchKeyVaultSecret(credentials);

Console.WriteLine("FETCHED Secret: test Value: {0}", sec);

while (true) {
    System.Threading.Thread.Sleep(1000);
}

// Gets a TokenCredential
static TokenCredential GetCredential(DefaultAzureCredentialOptions options)
{
    var creds = new DefaultAzureCredential(options);
    return creds;
}

// Reads KV Secret using MSI provided via the AzWi flow on Kubernetes
static async Task<string> FetchKeyVaultSecret(TokenCredential tokenCredential) 
{
    var vaultUri = "AKV URL";
    SecretClient secretClient = new SecretClient(new Uri(vaultUri), tokenCredential);
    var secretBundle = await secretClient.GetSecretAsync("test");
    var secVal = secretBundle.Value.Value;
    return secVal;
}