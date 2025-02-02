namespace UploadBigAssetsSample.Security
{
    internal struct ClientCredentials
    {
        public ClientCredentials(string clientId, string secret)
        {
            ClientId = clientId;
            Secret = secret;
        }

        public string ClientId { get; set; }
        public string Secret { get; set; }

        public string Serialize()
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{ClientId}:{Secret}"));
        }
    }
}
