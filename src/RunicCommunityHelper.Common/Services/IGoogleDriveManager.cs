namespace RunicCommunityHelper.Common.Services;

public interface IGoogleDriveManager
{
    Task AddUserAsEditorAsync(string fileId, string userEmail);
}