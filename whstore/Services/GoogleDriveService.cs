using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace whstore.Services
{
    public class GoogleDriveService
    {
        private readonly DriveService? _driveService;
        private readonly string? _folderId;
        private bool _isInitialized = false;

        public bool IsInitialized => _isInitialized;

        public GoogleDriveService(IConfiguration config)
        {
            try
            {
                _folderId = config["GoogleDrive:FolderId"] ?? Environment.GetEnvironmentVariable("GOOGLE_DRIVE_FOLDER_ID");
                string? serviceAccountJson = config["GoogleDrive:ServiceAccountJson"] ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");
                string? relativePath = config["GoogleDrive:ServiceAccountFilePath"]
                    ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")
                    ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_FILE");

                if (string.IsNullOrWhiteSpace(_folderId))
                {
                    Console.WriteLine("⚠️ ERROR: GoogleDrive FolderId is not configured.");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(serviceAccountJson))
                {
                    var serviceCredential = CredentialFactory.FromJson<ServiceAccountCredential>(serviceAccountJson)
                        .ToGoogleCredential()
                        .CreateScoped(DriveService.Scope.Drive);

                    _driveService = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = serviceCredential,
                        ApplicationName = "wh-store"
                    });

                    _isInitialized = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    Console.WriteLine("⚠️ ERROR: GoogleDrive service account file path is not configured.");
                    return;
                }

                string jsonFilePath = Path.Combine(AppContext.BaseDirectory, relativePath);

                if (!File.Exists(jsonFilePath))
                {
                    // প্রকল্প রুটে খোঁজ
                    var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.FullName;
                    if (!string.IsNullOrEmpty(projectRoot))
                    {
                        var alternatePath = Path.Combine(projectRoot, relativePath);
                        if (File.Exists(alternatePath))
                        {
                            jsonFilePath = alternatePath;
                        }
                    }
                }

                if (!File.Exists(jsonFilePath))
                {
                    Console.WriteLine($"⚠️ ERROR: JSON file not found at: {jsonFilePath}");
                    return;
                }

                string fileJson = File.ReadAllText(jsonFilePath);
                var fileCredential = CredentialFactory.FromJson<ServiceAccountCredential>(fileJson)
                    .ToGoogleCredential()
                    .CreateScoped(DriveService.Scope.Drive);

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = fileCredential,
                    ApplicationName = "wh-store"
                });

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ GoogleDriveService Initialization Error: {ex.Message}");
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType = "image/jpeg")
        {
            // সার্ভিস ইনিশিয়ালাইজ হয়েছে কি না চেক করা
            if (!_isInitialized || _driveService == null || string.IsNullOrWhiteSpace(_folderId))
            {
                throw new Exception("Google Drive Service is not initialized. Please check JSON path and GoogleDrive settings.");
            }

            try
            {
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = fileName,
                    Parents = new List<string> { _folderId }
                };

                var request = _driveService.Files.Create(fileMetadata, fileStream, contentType);
                request.Fields = "id";

                var response = await request.UploadAsync();

                if (response.Status == Google.Apis.Upload.UploadStatus.Completed)
                {
                    return request.ResponseBody.Id;
                }
                else
                {
                    // বিস্তারিত এরর মেসেজ পাওয়ার জন্য
                    string errorMsg = response.Exception != null ? response.Exception.Message : response.Status.ToString();
                    throw new Exception($"File upload failed: {errorMsg}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error during Google Drive upload: {ex.Message}");
            }
        }
    }
}