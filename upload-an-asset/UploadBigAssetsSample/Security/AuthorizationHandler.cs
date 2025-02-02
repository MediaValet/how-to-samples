using System.Net.Http.Headers;
using System.Text.Json;

namespace UploadBigAssetsSample.Security
{
    internal class AuthorizationHandler
    {
        const string Scope = "openid api";
        const string GrantType = "password";
        static HttpClient httpClient = new HttpClient();
        readonly string _authorizationHeader;
        readonly ClientCredentials _clientCredentials;
        readonly string _uri = "https://login.mediavalet.com/connect/token";

        public AuthorizationHandler(ClientCredentials clientCredentials)
        {
            _clientCredentials = clientCredentials;
            _authorizationHeader = $"{clientCredentials.Serialize()}";
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authorizationHeader);
        }

        public async Task<string> GetNewAccessToken(UserCredentials userCredentials)
        {
            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientCredentials.ClientId),
                new KeyValuePair<string, string>("grant_type", GrantType),
                new KeyValuePair<string, string>("username", userCredentials.userName),
                new KeyValuePair<string, string>("password", userCredentials.password),
                new KeyValuePair<string, string>("scope", Scope)
            });
            string responseBody = null;
            try
            {
                // Make the POST request
                HttpResponseMessage response = await httpClient.PostAsync(_uri, body);
                responseBody = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode(); // Throw an exception if the response is not successful

                // Parse the response content

                // Extract the access_token from the response (assuming JSON format)
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var accessToken = jsonResponse.GetProperty("access_token").GetString();
                //return accessToken;
                Console.WriteLine("Access Token: " + accessToken);
                return accessToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return string.Empty;
            }
        }
    }
}
