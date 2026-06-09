using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace whstore.Services
{
    public class GoogleDriveService
    {
        private readonly DriveService _driveService;
        private readonly string _folderId;
        private bool _isInitialized = false; // readonly সরানো হয়েছে

        public GoogleDriveService(IConfiguration config)
        {
            try
            {
                _folderId = config["GoogleDrive:FolderId"];
                string relativePath = config["GoogleDrive:ServiceAccountFilePath"];
                string jsonFilePath = Path.Combine(AppContext.BaseDirectory, relativePath);

                if (!File.Exists(jsonFilePath))
                {
                    // ফাইল না পেলে এটি কনসোলে দেখাবে
                    Console.WriteLine($"⚠️ ERROR: JSON file not found at: {jsonFilePath}");
                    return;
                }

                GoogleCredential credential;
                using (var stream = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(DriveService.Scope.Drive);
                }

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
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
            if (!_isInitialized || _driveService == null)
            {
                throw new Exception("Google Drive Service is not initialized. Please check JSON path and 'Copy Always' settings.");
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