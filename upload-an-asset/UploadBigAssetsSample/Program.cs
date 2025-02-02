using Azure.Storage;
using Azure.Storage.Blobs;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using UploadBigAssetsSample.ApiContracts;
using UploadBigAssetsSample.Security;

namespace UploadBigAssetsSample
{
    internal class Program
    {
        private static ConcurrentBag<string> logs = new ConcurrentBag<string>();
        private static BlockingCollection<(string msg, string type)> logsQueue = new BlockingCollection<(string msg, string type)>();
        private const string Error = "ERROR";

        static Task AsyncLogWritter(CancellationToken cancellationToken)
        {
            var loggerInConsole = Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested && !logsQueue.IsCompleted)
                {
                    try
                    {
                        var msg = logsQueue.Take(cancellationToken);
                        var current = Console.ForegroundColor;
                        if (msg.type == Error)
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine(msg.msg);
                        Console.ForegroundColor = current;
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }, cancellationToken);
            return loggerInConsole;
        }

        static async Task Main(string[] args)
        {
            var forceSync = false;// args.Contains("runsync");
            var cancellationTokenSource = new CancellationTokenSource();
            var logWritterTask = AsyncLogWritter(cancellationTokenSource.Token);
            Console.WriteLine("Enter with your OCP Key (CS team can provide you)");
            var ocpKey = Console.ReadLine();
            Console.WriteLine("Enter with your Library Id (CS team can provide you)");
            var libraryId = Console.ReadLine();
            Console.WriteLine("Enter with a valid category Id");
            var categoryId = Console.ReadLine();
            Console.WriteLine("Enter the file path");
            var bigFilePath = Console.ReadLine();
            Console.WriteLine("Enter with your client Id (CS team can provide you)");
            var clientId = Console.ReadLine();
            Console.WriteLine("Enter with your secret (CS team can provide you)");
            var secret = Console.ReadLine();
            Console.WriteLine("Enter with your User name (Same you use in our portal)");
            var userName = Console.ReadLine();
            Console.WriteLine("Enter with your user password (Same you use in our portal)");
            var userPassword = Console.ReadLine();

            // first using the PasswordFlow to get required credentials

            // you can request our CS team to send this information to you
            var handler = new AuthorizationHandler(new ClientCredentials(clientId, secret));
            var token = await handler.GetNewAccessToken(new UserCredentials(userName, userPassword));
            Log($"Token acquired {token}");
            FileInfo fileInfo = new FileInfo(bigFilePath);
            Log("Creating Api Client");
            var apiClient = CreateHttpClient("https://api.mediavalet.com/", ocpKey, token);
            Log("Requesting access to upload an asset");
            // to start the upload we need the access to upload an asset
            var uploadInfo = await RequestUploadAssetAsync(apiClient, fileInfo);
            Log("Uploading blob to azure");
            await UploadBlobAsync(uploadInfo, fileInfo);
            // upload blob is a required step to run other steps

            Log("Filling all properties for the asset");
            var fillAssetTask = FillAssetPlaceholderAsync(apiClient, uploadInfo, fileInfo);
            if (forceSync)
                await fillAssetTask;
            Log("Setting default category for the asset");
            var setAssetCategoryTask = SetDefaultAssetCategoryAsync(apiClient, uploadInfo, fileInfo, categoryId);
            if (forceSync)
                await setAssetCategoryTask;
            await Task.WhenAll(fillAssetTask, setAssetCategoryTask);
            // set category and fill asset can run in parallel
            Log("Completting upload");
            await CompleteUploadAsync(apiClient, uploadInfo, fileInfo);
            Log("Upload Completed");
            cancellationTokenSource.Cancel();
            await logWritterTask;
        }

        static HttpClient CreateHttpClient(string apiUrl, string yuorOcpKey, string jwtToken)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(apiUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", jwtToken);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", yuorOcpKey);
            return client;
        }

        static async Task<AssetAUploadAccessInfo> RequestUploadAssetAsync(HttpClient apiClient, FileInfo fileInfo)
        {
            var responseUpload = await apiClient.PostAsJsonAsync($"uploads", new
            {
                filename = fileInfo.Name
            });
            try
            {
                responseUpload.EnsureSuccessStatusCode();
                var jsonUpload = await responseUpload.Content.ReadFromJsonAsync<RequestEnvelope<AssetAUploadAccessInfo>>();
                return jsonUpload.Payload;
            }
            catch (Exception)
            {
                Log(responseUpload, $"[{responseUpload.StatusCode}] - An error occurred on the request to get an Url to upload the asset, {responseUpload.ReasonPhrase}");
                throw;
            }
        }

        private static async Task UploadBlobAsync(AssetAUploadAccessInfo uploadInfo, FileInfo localFile)
        {
            string blobAuthenticatedPath = uploadInfo.UploadUrl;
            Uri blobUri = new Uri(blobAuthenticatedPath);

            try
            {
                BlobClient blobClient = new BlobClient(new Uri(uploadInfo.UploadUrl));
                await blobClient.UploadAsync(localFile.OpenRead());
                Log($"File uploaded at blob storage {localFile.Name}");
            }
            catch (Exception ex)
            {
                Log(ex);
                throw;
            }
        }

        private static async Task FillAssetPlaceholderAsync(HttpClient apiClient, AssetAUploadAccessInfo uploadInfo, FileInfo fileInfo)
        {
            var response = await apiClient.PutAsJsonAsync($"/uploads/{uploadInfo.Id}", new
            {
                filename = uploadInfo.UploadFilename,
                title = fileInfo.Name,
                description = string.Empty,
                fileSizeInBytes = fileInfo.Length
            });
            try
            {
                response.EnsureSuccessStatusCode();
                Log($"Asset data updated to file {fileInfo.Name} - with AssetId = {uploadInfo.Id}");
            }
            catch (Exception ex)
            {
                Log(response, $"[{response.StatusCode}] - An error occurrend on the request to update asset infos, {response.ReasonPhrase}");
                throw;
            }
        }

        private static async Task SetDefaultAssetCategoryAsync(HttpClient apiClient, AssetAUploadAccessInfo uploadInfo, FileInfo fileInfo, string category)
        {
            var responseAddAssetOnCategory = await apiClient.PostAsJsonAsync($"/uploads/{uploadInfo.Id}/categories", new[]
            {
                category
            });

            try
            {
                responseAddAssetOnCategory.EnsureSuccessStatusCode();
                Log($"Asset category updated to file {fileInfo.Name} - with AssetId = {uploadInfo.Id}");
            }
            catch (Exception)
            {
                Log(responseAddAssetOnCategory, $"[{responseAddAssetOnCategory.StatusCode}] - An error occurrend on the request to add category on asset, {responseAddAssetOnCategory.ReasonPhrase}");
                throw;
            }
        }

        private static async Task CompleteUploadAsync(HttpClient apiClient, AssetAUploadAccessInfo uploadInfo, FileInfo fileInfo)
        {
            var responseFinalizeAsset = await apiClient.PatchAsJsonAsync($"/uploads/{uploadInfo.Id}", new[]
            {
                new
                {
                    op = "replace",
                    path = "/status",
                    value = 1
                }
            });
            try
            {
                responseFinalizeAsset.EnsureSuccessStatusCode();
                Log($"Asset upload finalized to file {fileInfo.Name} - with AssetId = {uploadInfo.Id}");
            }
            catch (Exception)
            {
                Log(responseFinalizeAsset, $"[{responseFinalizeAsset.StatusCode}] - An error occurrend on the request to finalize upload to asset, {responseFinalizeAsset.ReasonPhrase}");
                throw;
            }
        }

        private static void Log(HttpResponseMessage responseMessage, string message, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "")
        {
            var msg = $"[{DateTime.Now}] - [{responseMessage.StatusCode}] [{sourceLineNumber} - {memberName} \"{sourceFilePath}\"]:\n\t{memberName}";
            logsQueue.Add((msg, Error));
            logs.Add(msg);
            if (responseMessage.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                Console.Beep();
            }
        }

        private static void Log(Exception exception, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "")
        {
            var msg = $"[{DateTime.Now}] - an exception occured at [{sourceLineNumber} - {memberName} \"{sourceFilePath}\"]:\n\t{exception.Message}";
            logsQueue.Add((msg, Error));
            logs.Add(msg);
        }

        private static void Log(string message, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "")
        {
            var msg = $"[{DateTime.Now}] - {message} at [{sourceLineNumber} - {memberName} \"{sourceFilePath}\"]";
            logsQueue.Add((msg, "INFO"));
            logs.Add(msg);
        }
    }
}
