using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;

public class AboutModel : PageModel
{
    public string Message { get; set; }

    public async Task OnGetAsync()
    {
        Message = "Your application description page.";
        int retries = 0;
        bool retry = false;
        try
        {
            /* The below 4 lines of code shows you how to use AppAuthentication library to fetch secrets from your Key Vault*/
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            var secret = await keyVaultClient.GetSecretAsync("https://<yourkvreprohere>.vault.azure.net/secrets/<yourkvreprohere>/secretValue")
                    .ConfigureAwait(false);
            Message = secret.Value;

            /* The below do while logic is to handle throttling errors thrown by Azure Key Vault. It shows how to do exponential backoff which is the recommended client side throttling*/
            do
            {
                long waitTime = Math.Min(getWaitTime(retries), 2000000);
                secret = await keyVaultClient.GetSecretAsync("https://<yourkvreprohere>.vault.azure.net/secrets/<yourkvreprohere>/secretValue")
                    .ConfigureAwait(false);
                retry = false;
            }
            while (retry && (retries++ < 10));
        }
        /// <exception cref="KeyVaultErrorException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        catch (KeyVaultErrorException keyVaultException)
        {
            Message = keyVaultException.Message;
            if ((int)keyVaultException.Response.StatusCode == 429)
                retry = true;
        }
    }

    // This method implements exponential backoff incase of 429 errors from Azure Key Vault
    private static long getWaitTime(int retryCount)
    {
        long waitTime = ((long)Math.Pow(2, retryCount) * 100L);
        return waitTime;
    }

    // This method fetches a token from Azure Active Directory which can then be provided to Azure Key Vault to authenticate
    public async Task<string> GetAccessTokenAsync()
    {
        var azureServiceTokenProvider = new AzureServiceTokenProvider();
        string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
        return accessToken;
    }
}