namespace UploadBigAssetsSample.ApiContracts
{
    internal class RequestEnvelope<T>
    {
        public string ApiVersion { get; set; }
        public T Payload { get; set; }
    }
}
