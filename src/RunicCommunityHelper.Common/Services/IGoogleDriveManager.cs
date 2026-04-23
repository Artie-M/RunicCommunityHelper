namespace RunicCommunityHelper.Common.Services;

public interface IGoogleDriveManager
{
    Task AddUserAsEditorAsync(string userEmail, params string[] fileIds);
    
    Task RemoveUserAccessAsync(string userEmail, params string[] fileIds);
}