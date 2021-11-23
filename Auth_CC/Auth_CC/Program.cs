using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Generic;
using System.Diagnostics;

namespace Auth_CC
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Step 1: get needed variables 
            IConfigurationBuilder builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json")
               .AddJsonFile("appsettings.test.json", optional: true);
            var _configuration = builder.Build();

            // ==== Client constants ====
            string tenantId = _configuration["TenantId"];
            string resource = _configuration["Resource"];
            string clientId = _configuration["ClientId"];
            string clientSecret = _configuration["ClientSecret"];
            string apiVersion = _configuration["ApiVersion"];

            using (var httpClient = new HttpClient())
            {
                // Step 2: get the authentication endpoint from the discovery URL
                var wellknown_information = await httpClient.GetFromJsonAsync<JsonElement>($"{resource}/identity/.well-known/openid-configuration");
                string token_url = wellknown_information.GetProperty("token_endpoint").GetString();

                // Step 3: use the client ID and Secret to get the needed bearer token
                var data = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                var token_information = await httpClient.PostAsync(token_url, data);
                var tokenObject = (await token_information.Content.ReadFromJsonAsync<JsonElement>());
                var token = tokenObject.GetProperty("access_token").GetString();

                // Step 4: test token by calling the base tenant endpoint 
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var test = await httpClient.GetAsync($"{resource}/api/{apiVersion}/Tenants/{tenantId}");
                if (!test.IsSuccessStatusCode) throw new Exception("Check Failed");
            }
        }
    }
}
