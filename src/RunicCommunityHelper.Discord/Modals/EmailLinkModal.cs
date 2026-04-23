using Discord;
using Discord.Interactions;

namespace RunicCommunityHelper.Discord.Modals;

public class EmailLinkModal : IModal
{
    public string Title => "Привязка почты";
    
    [InputLabel("Почта вашего Google-аккаунта")]
    [ModalTextInput("email_input", TextInputStyle.Short, minLength: 2, maxLength: 128)]
    public required string Email { get; set; }
}