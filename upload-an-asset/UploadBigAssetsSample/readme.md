
# Uploading Large Assets to MediaValet API

## Overview

This example demonstrates how to upload assets larger than **64MB** to **MediaValet API** using **Azure Blob Storage** as the permanent storage repository for the physical file. The approach ensures efficient handling of large files while complying with MediaValet’s API limitations. Since operations typically do not accept files larger than **64MB**, this implementation uses **chunked uploads**, leveraging the Microsoft Azure SDK to handle this efficiently.

## How It Works

1. **Authenticate with MediaValet API** using OAuth2 (`AuthorizationHandler.cs`). Currently, only **password flow** is supported.
2. **Request a SAS Upload URL** from MediaValet API (`AssetAUploadAccessInfo.cs`).
3. **Upload the asset in chunks** to Azure Blob Storage (`Program.cs`).
4. **Send metadata update request** to the MediaValet API to populate additional asset details.
5. **Set asset category** in MediaValet API.
6. **Change asset status** via the API to finalize the upload process.

## Prerequisites

Before running this example, make sure you have:

- A **MediaValet API key** and valid credentials.
- **.NET 8 SDK** installed.

## Running the Example

1. **Clone the repository and navigate to the project directory**:
   ```sh
   git clone https://github.com/your-username/mediavalet-api-examples.git
   cd mediavalet-api-examples/UploadBigAssetsSample
   ```
2. **Restore dependencies**:
   ```sh
   dotnet restore
   ```
3. **Run the application and follow the prompts**:
   ```sh
   dotnet run
   ```

## Code Breakdown

### Authentication (`AuthorizationHandler.cs`)

- Uses OAuth2 for authentication.
- Requests an access token from MediaValet's authentication endpoint.
- Implements `ClientCredentials` struct for secure handling.
- Currently supports **password flow** authentication only.

### Upload Process

1. **Request SAS URL from MediaValet API** (`AssetAUploadAccessInfo.cs`).
2. **Use Azure SDK for chunked upload** (`Program.cs`).
3. **Notify MediaValet API after upload completion**.
4. **Update metadata and set category**.
5. **Change asset status to complete the process**.

### API Contracts (`AssetAUploadAccessInfo.cs`)

- Defines `AssetAUploadAccessInfo` model to store upload session details.
- Holds the upload URL and filename returned by MediaValet.

## Next Steps

- Implement **progress tracking** for large file uploads.
- Optimize chunk size handling for different file formats.
- Add support for **resumable uploads** in case of failures.

## Resources

- [MediaValet API Documentation](https://docs.mediavalet.com/)
- [Azure Storage SDK for .NET](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage)

---

This documentation can be extended based on specific implementation needs or additional features.

