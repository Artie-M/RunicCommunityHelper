using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using RunicCommunityHelper.Common.Services;
using Serilog;

namespace RunicCommunityHelper.Implementations.Services;

public class GoogleDriveManager : IGoogleDriveManager
{
    private readonly string[] _scopes = { DriveService.Scope.Drive };
    private readonly string _applicationName = "RunicCommunityHelper";
    private readonly string _jsonKeyFilePath = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_PATH") ?? "google-key.json";
    
    public async Task AddUserAsEditorAsync(string userEmail, params string[] fileIds)
    {
        var service = GetDriveService();
        
        Permission newPermission = new Permission
        {
            Type = "user",
            Role = "writer",
            EmailAddress = userEmail
        };

        foreach (var fileId in fileIds)
        {
            var userPermission = await CheckPermission(service, fileId, userEmail);
            
            if (userPermission != null && userPermission.Role == "writer")
            {
                Log.Information($"У пользователя {userEmail} уже есть права редактора на документ {fileId}");
                continue;
            }
            
            await AssignUserToDocument(service, newPermission, userEmail, fileId);
        }
    }
    
    public async Task RemoveUserAccessAsync(string userEmail, params string[] fileIds)
    {
        var service = GetDriveService();
        foreach (var fileId in fileIds)
        {
            try
            {
                var existingPermission = await CheckPermission(service, fileId, userEmail);
                if (existingPermission == null) continue;

                if (existingPermission.Role == "owner")
                {
                    Log.Warning($"Пользователь {userEmail} является владельцем документа {fileId}, доступ не отозван");
                    continue;
                }

                await RemoveUserFromDocument(service, existingPermission, userEmail, fileId);
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка при проверке доступа у {userEmail} к файлу {fileId}: {ex.Message}");
            }
        }
    }

    private DriveService GetDriveService()
    {
        GoogleCredential credential;
        using (var stream = new FileStream(_jsonKeyFilePath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream).CreateScoped(_scopes);
        }

        return new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = _applicationName,
        });
    }
    
    private async Task AssignUserToDocument(DriveService service, Permission newPermission, string userEmail, string fileId)
    {
        try
        {
            var request = service.Permissions.Create(newPermission, fileId);
            request.SendNotificationEmail = true;
            request.SupportsAllDrives = true;
            
            await request.ExecuteAsync();
            Log.Information($"Доступ успешно предоставлен пользователю: {userEmail}");
        }
        catch (Exception ex)
        {
            Log.Information($"Ошибка при добавлении доступа: {ex.Message}");
        }
    }

    private async Task RemoveUserFromDocument(DriveService service, Permission existingPermission, string userEmail, string fileId)
    {
        try
        {
            var request = service.Permissions.Delete(fileId, existingPermission.Id);
            request.SupportsAllDrives = true;

            await request.ExecuteAsync();
            Log.Information($"Права доступа успешно отозваны у пользователя: {userEmail} для документа {fileId}");
        }
        catch (Exception ex)
        {
            Log.Error($"Ошибка при удалении доступа у {userEmail} к файлу {fileId}: {ex.Message}");
        }
    }
    
    private async Task<Permission?> CheckPermission(DriveService service, string fileId, string userEmail)
    {
        var listRequest = service.Permissions.List(fileId);
        listRequest.Fields = "permissions(id, emailAddress, role)";
        listRequest.SupportsAllDrives = true;
        var existingPermissions = await listRequest.ExecuteAsync();

        return existingPermissions.Permissions?.FirstOrDefault(p => 
            string.Equals(p.EmailAddress, userEmail, StringComparison.OrdinalIgnoreCase));
    }
}