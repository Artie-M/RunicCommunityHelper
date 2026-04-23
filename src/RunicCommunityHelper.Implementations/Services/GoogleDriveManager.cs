using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using RunicCommunityHelper.Common.Services;
using Serilog;

namespace RunicCommunityHelper.Implementations.Services;

public class GoogleDriveManager : IGoogleDriveManager
{
    private readonly string[] _scopes = { DriveService.Scope.DriveFile };
    private readonly string _applicationName = "RunicCommunityHelper";
    private readonly string _jsonKeyFilePath = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_PATH") ?? "google-key.json";
    
    public async Task AddUserAsEditorAsync(string fileId, string userEmail)
    {
        GoogleCredential credential;
        using (var stream = new FileStream(_jsonKeyFilePath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream).CreateScoped(_scopes);
        }

        var service = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = _applicationName,
        });
        
        Permission newPermission = new Permission
        {
            Type = "user",
            Role = "writer",
            EmailAddress = userEmail
        };

        try
        {
            var request = service.Permissions.Create(newPermission, fileId);
            request.SendNotificationEmail = true; 
            
            var result = await request.ExecuteAsync();
            Log.Information($"Доступ успешно предоставлен пользователю: {userEmail}");
        }
        catch (Exception ex)
        {
            Log.Information($"Ошибка при добавлении доступа: {ex.Message}");
        }
    }
}