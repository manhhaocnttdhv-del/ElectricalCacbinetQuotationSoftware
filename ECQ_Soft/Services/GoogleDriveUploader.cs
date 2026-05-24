using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using ECQ_Soft.Properties;
using Newtonsoft.Json.Linq;

namespace ECQ_Soft.Services
{
    /// <summary>
    /// Helper upload file lên Google Drive sử dụng Service Account credential.
    /// File upload sẽ được đặt trong folder chỉ định (hoặc root nếu không có folder).
    /// </summary>
    public class GoogleDriveUploader
    {
        private DriveService _driveService;
        private string _folderId; // Folder ID trên Drive để lưu file
        private const string DEFAULT_FOLDER_NAME = "file vnecco";

        /// <summary>
        /// Khởi tạo uploader với folder ID tùy chọn.
        /// Nếu folderId = null, sẽ tự động tìm/tạo folder "file vnecco".
        /// </summary>
        public GoogleDriveUploader(string folderId = null)
        {
            _folderId = folderId;
            InitDriveService();
        }

        /// <summary>
        /// Đảm bảo folder "file vnecco" tồn tại trên Drive.
        /// Nếu chưa có sẽ tạo mới. Trả về folder ID.
        /// </summary>
        private async Task EnsureFolderAsync()
        {
            if (!string.IsNullOrEmpty(_folderId)) return;

            // Tìm folder "file vnecco" đã tồn tại
            var listReq = _driveService.Files.List();
            listReq.Q = $"name = '{DEFAULT_FOLDER_NAME}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
            listReq.Fields = "files(id, name)";
            listReq.Spaces = "drive";

            var result = await listReq.ExecuteAsync();
            if (result.Files != null && result.Files.Count > 0)
            {
                _folderId = result.Files[0].Id;
                return;
            }

            // Chưa có → tạo folder mới
            var folderMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = DEFAULT_FOLDER_NAME,
                MimeType = "application/vnd.google-apps.folder"
            };

            var createReq = _driveService.Files.Create(folderMetadata);
            createReq.Fields = "id";
            var folder = await createReq.ExecuteAsync();
            _folderId = folder.Id;
        }

        private void InitDriveService()
        {
            GoogleCredential credential = null;
            Exception lastEx = null;

            string[] credentialsToTry = {
                Resources.GoogleCredentialJson1,
                Resources.GoogleCredentialJson2
            };

            foreach (var jsonStr in credentialsToTry)
            {
                if (string.IsNullOrWhiteSpace(jsonStr)) continue;
                string trimmed = jsonStr.Trim();

                try
                {
                    if (trimmed.StartsWith("["))
                    {
                        var arr = JArray.Parse(trimmed);
                        foreach (var item in arr)
                        {
                            try
                            {
                                string itemStr = item.ToString();
                                // Chỉ parse nếu là JSON object có "type" field
                                if (!itemStr.TrimStart().StartsWith("{")) continue;

                                var tempCredential = GoogleCredential.FromJson(itemStr)
                                    .CreateScoped(DriveService.Scope.DriveFile, DriveService.Scope.Drive);

                                // Test xem credential có hoạt động không
                                var tempService = new DriveService(new BaseClientService.Initializer()
                                {
                                    HttpClientInitializer = tempCredential,
                                    ApplicationName = "ECQ_Soft_DriveUploader",
                                });

                                // Thử list 1 file để kiểm tra quyền
                                var testReq = tempService.Files.List();
                                testReq.PageSize = 1;
                                testReq.Fields = "files(id)";
                                testReq.Execute();

                                // Nếu thành công, giữ credential
                                credential = tempCredential;
                                _driveService = tempService;
                                break;
                            }
                            catch (Exception ex) { lastEx = ex; credential = null; }
                        }
                    }
                    else
                    {
                        if (trimmed.TrimStart().StartsWith("{"))
                        {
                            credential = GoogleCredential.FromJson(trimmed)
                                .CreateScoped(DriveService.Scope.DriveFile, DriveService.Scope.Drive);
                        }
                    }

                    if (credential != null) break;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                }
            }

            if (credential == null)
                throw lastEx ?? new Exception("Không thể khởi tạo Google Drive credential. Kiểm tra lại Service Account JSON.");

            if (_driveService == null)
            {
                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "ECQ_Soft_DriveUploader",
                });
            }
        }

        /// <summary>
        /// Upload một file lên Google Drive (folder "file vnecco").
        /// Trả về link xem file (webViewLink).
        /// </summary>
        public async Task<DriveUploadResult> UploadFileAsync(string localFilePath)
        {
            if (!File.Exists(localFilePath))
                throw new FileNotFoundException("File không tồn tại: " + localFilePath);

            // Đảm bảo folder "file vnecco" tồn tại
            await EnsureFolderAsync();

            string fileName = Path.GetFileName(localFilePath);
            string mimeType = GetMimeType(localFilePath);

            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                MimeType = mimeType
            };

            // Đặt vào folder "file vnecco"
            if (!string.IsNullOrEmpty(_folderId))
            {
                fileMetadata.Parents = new List<string> { _folderId };
            }

            using (var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
            {
                var request = _driveService.Files.Create(fileMetadata, stream, mimeType);
                request.Fields = "id, name, webViewLink, webContentLink";

                var progress = await request.UploadAsync();

                if (progress.Status == UploadStatus.Failed)
                    throw new Exception($"Upload thất bại: {progress.Exception?.Message}");

                var uploadedFile = request.ResponseBody;

                // Set permission: anyone with link can view
                var permission = new Google.Apis.Drive.v3.Data.Permission
                {
                    Type = "anyone",
                    Role = "reader"
                };
                await _driveService.Permissions.Create(permission, uploadedFile.Id).ExecuteAsync();

                // Lấy lại file info với webViewLink
                var getReq = _driveService.Files.Get(uploadedFile.Id);
                getReq.Fields = "id, name, webViewLink, webContentLink";
                var fileInfo = await getReq.ExecuteAsync();

                return new DriveUploadResult
                {
                    FileId = fileInfo.Id,
                    FileName = fileInfo.Name,
                    WebViewLink = fileInfo.WebViewLink ?? $"https://drive.google.com/file/d/{fileInfo.Id}/view",
                    WebContentLink = fileInfo.WebContentLink
                };
            }
        }

        /// <summary>
        /// Upload nhiều file cùng lúc. Trả về danh sách kết quả.
        /// </summary>
        public async Task<List<DriveUploadResult>> UploadMultipleFilesAsync(string[] filePaths, IProgress<int> progress = null)
        {
            var results = new List<DriveUploadResult>();
            for (int i = 0; i < filePaths.Length; i++)
            {
                var result = await UploadFileAsync(filePaths[i]);
                results.Add(result);
                progress?.Report(i + 1);
            }
            return results;
        }

        private static string GetMimeType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            switch (ext)
            {
                case ".pdf": return "application/pdf";
                case ".png": return "image/png";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".bmp": return "image/bmp";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".xls": return "application/vnd.ms-excel";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".doc": return "application/msword";
                case ".pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case ".txt": return "text/plain";
                case ".csv": return "text/csv";
                case ".zip": return "application/zip";
                case ".rar": return "application/x-rar-compressed";
                case ".dwg": return "application/acad";
                case ".dxf": return "application/dxf";
                default: return "application/octet-stream";
            }
        }
    }

    public class DriveUploadResult
    {
        public string FileId { get; set; }
        public string FileName { get; set; }
        public string WebViewLink { get; set; }
        public string WebContentLink { get; set; }
    }
}
