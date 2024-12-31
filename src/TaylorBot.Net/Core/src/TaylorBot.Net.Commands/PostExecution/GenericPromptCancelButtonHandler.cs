using TaylorBot.Net.Core.Embed;

namespace TaylorBot.Net.Commands.PostExecution;

public class GenericPromptCancelButtonHandler(InteractionResponseClient responseClient) : IButtonHandler
{
    public static string CustomIdName => "pc";

    public IComponentHandlerInfo Info => new MessageHandlerInfo(CustomIdName);

    public async Task HandleAsync(DiscordButtonComponent button)
    {
        await responseClient.EditOriginalResponseAsync(button, EmbedFactory.CreateErrorEmbed("Operation cancelled 👍"));
    }
}
