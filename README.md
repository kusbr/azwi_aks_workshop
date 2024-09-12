# Workshop- Azure Workload Identity in AKS

## Steps
1. Setup AKS with AzWi readiness (OIDC Issuer)
    - Simple 1 node configuration
    - No redundancy
    - No monitoring
    - No prometheus metrics
    - ACR (your choice)  
    - Enable Workload Identity on AKS Security Profile

     
           az aks update  -n $AKS_NAME -g $RESOURCE_GROUP --enable-managed-identity --y

    - Enable OIDC Issuer Profile

            az aks update  -n $AKS_NAME -g $RESOURCE_GROUP --enable-oidc-issuer

    - Copy the OIDC Issuer URL


            oidcIssuerProfile
                issuerUrl

1. Setup a KeyVault resource (Protected resource)
    - Use Azure RBAC
    - Disable Vault Access Policy 
    - Provide yourself KV Administrator Role Assignment (IAM)
    - Create a new secret (manual)

            Secret name: test
            Secert Value: test-secert-value
            ContentType: (appropriate)
            ExpirationDate: (appropriate) 

1. Provision and Configure uMSI (Identity to access the resource) 

    - User Managed System Identity
    - Provide KVAdmin roleassignment for this uMSI on the Keyvault resource  


1. Build a simple workload to access the KeyVault secret 

    - Console:
    
      
          dotnet new console -n workload

    - File: workload/workload.csproj

                <ItemGroup>
                    <PackageReference Include="Azure.Identity" Version="1.11.4" />
                    <PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.6.0" />
                </ItemGroup>


    - File: workload/Program.cs
      
                using Azure.Core;
                using Azure.Identity;
                using Azure.Security.KeyVault.Secrets;

                Console.WriteLine("AZURE WORKLOAD IDENITY IN AKS");

                DefaultAzureCredentialOptions options = new();

                var credentials = GetCredential(options);

                var sec = await FetchKeyVaultSecret(credentials);

                Console.WriteLine("FETCHED Secret: test Value: {0}", sec);


                // Gets a TokenCredential
                static TokenCredential GetCredential(DefaultAzureCredentialOptions options)
                {
                    var creds = new DefaultAzureCredential(options);
                    return creds;
                }

                // Reads KV Secret using MSI provided via the AzWi flow on Kubernetes
                static async Task<string> FetchKeyVaultSecret(TokenCredential tokenCredential) 
                {
                    var vaultUri = "https://kv-k8s-wi-demo.vault.azure.net/";
                    SecretClient secretClient = new SecretClient(new Uri(vaultUri), tokenCredential);
                    var secretBundle = await secretClient.GetSecretAsync("test");
                    var secVal = secretBundle.Value.Value;
                    return secVal;
                }

    - Console

            dotnet sln add ./workload/workload.csproj
            cd ./workload
            dotnet run

            // See errors

1. Containerize the workload
    - FILE: workload/Dockerfile

            FROM mcr.microsoft.com/dotnet/aspnet:8.0-cbl-mariner AS base
            WORKDIR /app

            FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
            WORKDIR /app
            RUN apt-get update -y && apt-get upgrade -y

            COPY . .
            RUN dotnet restore ./workload.csproj
            RUN dotnet publish ./workload.csproj -c Release -o /build  --no-restore

            FROM base AS final
            WORKDIR /app
            COPY --from=build /build .

            ENTRYPOINT ["dotnet", "workload.dll"]

    - Console:


            docker build -t {your acr or CR path}/azwidemoworkload:1.0 .

            docker push {image name}


1. Create K8s manifests to use AzWI for the workload

    - File: workload/k8s/serviceAccount.yaml

            apiVersion: v1
            kind: ServiceAccount
            metadata:
            annotations:
                azure.workload.identity/client-id: {client Id of the uMSI}
                azure.workload.identity/tenant-id: {tenant id of the sub where the uMSI was provisioned}
                name: workload
                namespace: test

    - FILE: workload/k8s/deployment.yaml

            apiVersion: apps/v1
            kind: Deployment
            metadata:
            name: workload
            namespace: test
            labels:
                app: workload
                azure.workload.identity/use: "true"
            spec:
            selector:
                matchLabels:
                    app: workload
            template:
                metadata:
                labels:
                    app: workload
                    azure.workload.identity/use: "true"
                spec:
                    serviceAccountName: workload
                containers:
                    - name: workload
                      image: " docker.io/kumsub/azwidemoworkload:1.0"
                      imagePullPolicy: Always


1. Deploy onto AKS

    - Follow [link](https://github.com/Azure/azure-workload-identity/blob/main/docs/book/src/installation/mutating-admission-webhook.md)

    - Console

            az aks get-credentials -n {your aks name} -g {your rg name}

        
    - Cosnole: Create namespace "test"

            kubectl create ns test


    - Console: Deploy workload

            kubectl apply -f ./k8s

2. Fix Errors

    - Add Federated credential to MSI