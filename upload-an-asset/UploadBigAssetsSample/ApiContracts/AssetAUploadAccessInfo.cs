namespace UploadBigAssetsSample.ApiContracts
{
    internal class AssetAUploadAccessInfo
    {
        public static AssetAUploadAccessInfo Invalid { get; } = new AssetAUploadAccessInfo { UploadFilename = string.Empty, UploadUrl = string.Empty };

        public Guid Id { get; set; }
        public required string UploadUrl { get; set; }
        public required string UploadFilename { get; set; }
    }
}
