using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;

namespace ARMRest
{
    class MainClass
    {
        public static async Task Main(string[] args)
        {
            var tenantId = "16cb8f34-d9af-4199-bd32-a49b5288dedb";
            var subscriptionId = "44a56867-bd84-42d5-bd0e-eb50c574e4fd";
            var resourceGroupName = "tapster";
            var accountName = "tapster";

            var url = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{accountName}?api-version=2018-11-01";

            var createBody =
                @"
                {
                  ""sku"": {
                    ""name"": ""Standard_RAGRS""
                  },
                  ""kind"": ""Storage"",
                  ""location"": ""westus"",
                  ""properties"": {
                    ""supportsHttpsTrafficOnly"": false,
                    ""encryption"": {
                        ""services"": {
                            ""file"": {
                                ""enabled"": true
                            },
                            ""blob"": {
                                ""enabled"": true
                            }
                        },
                        ""keySource"": ""Microsoft.Storage""
                    }
                  }
                }";
            var createJson = JObject.Parse(createBody);

            var authContext = new AuthenticationContext($"https://login.microsoftonline.com/{tenantId}");
            var acquireTokenResults =
                await authContext.AcquireTokenAsync(
                    "https://management.azure.com/",
                    "1950a258-227b-4e31-a9cf-717495945fc2",
                    new Uri("urn:ietf:wg:oauth:2.0:oob"),
                    new PlatformParameters(PromptBehavior.Auto));


            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", acquireTokenResults.AccessToken);

            var httpClient = new HttpClient();
            var getResponse = await httpClient.SendAsync(request);
            var getBody = await getResponse.Content.ReadAsStringAsync();
            var getJson = JObject.Parse(getBody);

            Console.WriteLine("The following paths are in the create message and show their equivalence in the get objects");
            WalkAndCompare(createJson, getJson);

            Console.WriteLine();

            Console.WriteLine("The following paths are in the get message, but not in the create message");
            WalkAndFindMissing(getJson, createJson);

            Console.WriteLine("Press any key to quit");
            Console.ReadKey();
        }

        private static void WalkAndCompare(JToken createObject, JToken getObject)
        {
            foreach (var child in createObject.Children())
            {
                if (child.Children().Any())
                    WalkAndCompare(child, getObject);
                else
                {
                    Console.WriteLine($"The value '{child.Path}' {(JToken.Equals(child, getObject.SelectToken(child.Path)) ? "is " : "is *NOT* ")}" +
                        $"set correctly in Azure");
                }
            }
        }

        private static void WalkAndFindMissing(JToken getObject, JToken createObject)
        {
            foreach (var child in getObject.Children())
            {
                if (child.Children().Any())
                    WalkAndFindMissing(child, createObject);
                else
                {
                    if (createObject.SelectToken(child.Path) == null)
                    {
                        Console.WriteLine(child.Path);
                    }
                }
            }
        }
    }
}
