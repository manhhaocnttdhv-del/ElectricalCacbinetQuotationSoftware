using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using System.Collections.Concurrent;
using System.IO;

namespace ECQ_Soft.Services
{
    public static class GoogleCredentialCache
    {
        private static readonly ConcurrentDictionary<string, GoogleCredential> _cache = 
            new ConcurrentDictionary<string, GoogleCredential>();

        public static GoogleCredential GetCredential(string fileName)
        {
            return _cache.GetOrAdd(fileName, name =>
            {
                using (var stream = new FileStream(name, FileMode.Open, FileAccess.Read))
                {
                    return GoogleCredential.FromStream(stream)
                        .CreateScoped(SheetsService.Scope.Spreadsheets);
                }
            });
        }
    }
}
