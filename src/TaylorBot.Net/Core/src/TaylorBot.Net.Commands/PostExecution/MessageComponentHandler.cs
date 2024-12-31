using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using TaylorBot.Net.Core.Client;
using TaylorBot.Net.Core.Embed;

namespace TaylorBot.Net.Commands.PostExecution;

public record InteractionCustomId(string RawId)
{
    public const char Separator = '|';

    public string[] Split() => RawId.Split(Separator);

    public string Name => Split()[0];

    public string Data => Split()[1];

    public static InteractionCustomId Create(string name, string data) => new($"{name}{Separator}{data}");
}

public record DiscordButtonComponent(
    string Id,
    string Token,
    InteractionCustomId CustomId,
    string MessageId,
    string UserId,
    string? GuildId,
    Interaction RawInteraction
) : IInteraction;

public interface IComponentHandlerInfo
{
    string CustomIdName { get; }
}
public record MessageHandlerInfo(string CustomIdName) : IComponentHandlerInfo;
public record ModalHandlerInfo(string CustomIdName) : IComponentHandlerInfo;

public interface IButtonComponentHandler
{
    IComponentHandlerInfo Info { get; }

    Task HandleAsync(DiscordButtonComponent button);
}

public interface IButtonHandler : IButtonComponentHandler
{
    abstract static string CustomIdName { get; }
}

public partial class MessageComponentHandler(IServiceProvider services, ILogger<MessageComponentHandler> logger)
{
    private readonly Dictionary<string, Func<DiscordButtonComponent, ValueTask>> _callbacks = [];

    private InteractionResponseClient CreateInteractionClient() => services.GetRequiredService<InteractionResponseClient>();

    public async ValueTask HandleAsync(Interaction interaction)
    {
        switch (interaction.data!.component_type!)
        {
            case 2:
                ArgumentNullException.ThrowIfNull(interaction.data.custom_id);
                ArgumentNullException.ThrowIfNull(interaction.message);

                DiscordButtonComponent button = new(
                    interaction.id,
                    interaction.token,
                    new(interaction.data.custom_id),
                    interaction.message.id,
                    interaction.user != null ? interaction.user.id : interaction.member!.user.id,
                    interaction.guild_id,
                    interaction
                );

                // TODO verify exists in interaction.message

                var handler = services.GetKeyedService<IButtonComponentHandler>(button.CustomId.Name);
                if (handler != null)
                {
                    // TODO: Try/catch with default modal error?
                    switch (handler.Info)
                    {
                        case MessageHandlerInfo _:
                            await CreateInteractionClient().SendComponentAckResponseWithoutLoadingMessageAsync(button);
                            await handler.HandleAsync(button);
                            break;

                        case ModalHandlerInfo _:
                            await handler.HandleAsync(button);
                            break;

                        default: throw new NotImplementedException(handler.GetType().FullName);
                    }
                }
                else if (button.CustomId.Name.Equals("mmmrmy", StringComparison.OrdinalIgnoreCase))
                {
                    await CreateInteractionClient().SendComponentAckResponseWithoutLoadingMessageAsync(button);
                    await ModMailReplyConfirmAsync(button);
                }
                else
                {
                    if (_callbacks.TryGetValue(button.CustomId.RawId, out var callback))
                    {
                        await CreateInteractionClient().SendComponentAckResponseWithoutLoadingMessageAsync(button);

                        await callback(button);
                    }
                    else
                    {
                        logger.LogWarning("Button component without callback: {Interaction}", interaction);
                    }
                }
                break;

            default:
                logger.LogWarning("Unknown component type: {Interaction}", interaction);
                break;
        }
    }

    private async Task ModMailReplyConfirmAsync(DiscordButtonComponent button)
    {
        // TODO new UserHasPermissionOrOwnerPrecondition(GuildPermission.BanMembers);

        var promptMessage = button.RawInteraction.message;
        ArgumentNullException.ThrowIfNull(promptMessage);

        var modmailEmbed = promptMessage.embeds.First(e => e.title?.Contains("Message from the moderation team", StringComparison.OrdinalIgnoreCase) == true);
        ArgumentNullException.ThrowIfNull(modmailEmbed);

        var promptDescription = promptMessage.embeds.Single(e => e.title is null).description;
        ArgumentNullException.ThrowIfNull(promptDescription);

        var userIdMatch = UserIdRegex().Match(promptDescription);
        if (!userIdMatch.Success)
        {
            throw new InvalidOperationException("User ID format is invalid.");
        }
        var userId = userIdMatch.Groups[1].Value;

        ArgumentNullException.ThrowIfNull(button.GuildId);

        var client = services.GetRequiredService<ITaylorBotClient>();
        var guildUser = await client.ResolveGuildUserAsync(button.GuildId, userId);
        ArgumentNullException.ThrowIfNull(guildUser);

        await guildUser.SendMessageAsync(embed: InteractionMapper.ToDiscordEmbed(modmailEmbed));

        await CreateInteractionClient().EditOriginalResponseAsync(button, message: new(EmbedFactory.CreateSuccess($"Message sent to user ✉️")));
    }

    [GeneratedRegex(@"<@(\d+)>")]
    private static partial Regex UserIdRegex();

    public void AddCallback(string customId, Func<DiscordButtonComponent, ValueTask> callback)
    {
        _callbacks.Add(customId, callback);
    }

    public void RemoveCallback(string customId)
    {
        _callbacks.Remove(customId);
    }
}
