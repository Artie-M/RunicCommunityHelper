namespace RunicCommunityHelper.Common.Exceptions;

public class UserException : Exception
{
    public UserException(string message) : base(message)
    {
    }

    public UserException() : base("Неправильное использование команды. Проверьте корректность данных")
    {
    }
}