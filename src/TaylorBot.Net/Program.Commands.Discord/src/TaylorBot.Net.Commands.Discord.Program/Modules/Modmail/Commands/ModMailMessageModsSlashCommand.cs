using Discord;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using TaylorBot.Net.Commands.Discord.Program.Modules.Modmail.Domain;
using TaylorBot.Net.Commands.Discord.Program.Options;
using TaylorBot.Net.Commands.Parsers;
using TaylorBot.Net.Commands.PostExecution;
using TaylorBot.Net.Commands.Preconditions;
using TaylorBot.Net.Core.Client;
using TaylorBot.Net.Core.Embed;
using TaylorBot.Net.Core.Logging;
using TaylorBot.Net.Core.Strings;
using static TaylorBot.Net.Commands.MessageResult;

namespace TaylorBot.Net.Commands.Discord.Program.Modules.Modmail.Commands;

public class ModMailMessageModsSlashCommand(
    ILogger<ModMailMessageModsSlashCommand> logger,
    IOptionsMonitor<ModMailOptions> modMailOptions,
    IModMailBlockedUsersRepository modMailBlockedUsersRepository,
    ModMailChannelLogger modMailChannelLogger) : ISlashCommand<ModMailMessageModsSlashCommand.Options>
{
    public ISlashCommandInfo Info => new MessageCommandInfo("modmail message-mods", IsPrivateResponse: true);

    public record Options(ParsedString message);

    public static readonly Color EmbedColor = new(255, 255, 240);

    public ValueTask<Command> GetCommandAsync(RunContext context, Options options)
    {
        return new(new Command(
            new(Info.Name),
            () =>
            {
                var guild = context.Guild?.Fetched;
                ArgumentNullException.ThrowIfNull(guild);

                var embed = new EmbedBuilder()
                    .WithColor(EmbedColor)
                    .WithTitle("Message")
                    .WithDescription(options.message.Value)
                    .AddField("From", context.User.FormatTagAndMention(), inline: true)
                    .WithFooter("Mod mail received", iconUrl: modMailOptions.CurrentValue.ReceivedLogEmbedFooterIconUrl)
                    .WithCurrentTimestamp()
                .Build();

                return new(CreatePrompt(
                    new([embed, EmbedFactory.CreateWarning($"Are you sure you want to send the above message to the moderation team of '{guild.Name}'?")]),
                    confirm: async () => new(await SendAsync())
                ));

                async ValueTask<Embed> SendAsync()
                {
                    var isBlocked = await modMailBlockedUsersRepository.IsBlockedAsync(guild, context.User);
                    if (isBlocked)
                    {
                        return EmbedFactory.CreateError("Sorry, the moderation team has blocked you from sending mod mail 😕");
                    }

                    var channel = await modMailChannelLogger.GetModMailLogAsync(guild);

                    if (channel != null)
                    {
                        try
                        {
                            await channel.SendMessageAsync(embed: embed, components: new ComponentBuilder()
                                .WithButton(customId: ModMailUserMessageReplyButtonHandler.CustomIdName, label: "Reply", emote: new Emoji("📨")).Build());
                            return EmbedFactory.CreateSuccess(
                                $"""
                                Message sent to the moderation team of '{guild.Name}' ✉️
                                If you're expecting a response, **make sure you're able to send & receive DMs from TaylorBot** ⚙️
                                """);
                        }
                        catch (Exception e)
                        {
                            logger.LogWarning(e, "Error occurred when sending mod mail in {Guild}:", guild.FormatLog());
                        }
                    }

                    return EmbedFactory.CreateError(
                        $"""
                        I was not able to send the message to the moderation team 😕
                        Make sure they have a moderation log set up with {context.MentionCommand("mod log set")} and TaylorBot has access to it 🛠️
                        """);
                }
            },
            Preconditions: [
                new InGuildPrecondition(botMustBeInGuild: true)
            ]
        ));
    }
}

public class ModMailUserMessageReplyButtonHandler(InteractionResponseClient responseClient) : IButtonHandler
{
    public static string CustomIdName => "mmmr";

    public IComponentHandlerInfo Info => new ModalHandlerInfo(CustomIdName);

    public async Task HandleAsync(DiscordButtonComponent button)
    {
        // TODO share logic with main command
        await responseClient.SendModalResponseAsync(button, new CreateModalResult(
            Id: ModMailUserMessageReplyModalHandler.CustomIdName,
            Title: "Send Mod Mail to User",
            TextInputs: [new TextInput(Id: "messagecontent", TextInputStyle.Paragraph, Label: "Message to user")],
            SubmitAction: _ => throw new NotImplementedException(),
            IsPrivateResponse: true
        ));
    }
}

public partial class ModMailUserMessageReplyModalHandler(Lazy<ITaylorBotClient> taylorBotClient, InteractionResponseClient responseClient) : IModalHandler
{
    public static string CustomIdName => "mmmrm";

    public ModalComponentHandlerInfo Info => new(IsPrivateResponse: true);

    public async Task HandleAsync(ModalSubmit submit)
    {
        // TODO share logic with main command
        // TODO new UserHasPermissionOrOwnerPrecondition(GuildPermission.BanMembers);

        var messageContent = submit.TextInputs.Single(t => t.CustomId == "messagecontent").Value;

        ArgumentNullException.ThrowIfNull(submit.GuildId);
        var guild = taylorBotClient.Value.ResolveRequiredGuild(submit.GuildId);

        var fromField = submit.RawInteraction.message!.embeds.Single().fields!.Single(f => f.name.Contains("from", StringComparison.OrdinalIgnoreCase)).value;
        var userIdMatch = UserIdRegex().Match(fromField);
        if (!userIdMatch.Success)
        {
            throw new InvalidOperationException("User ID format is invalid.");
        }
        var userId = userIdMatch.Groups[1].Value;

        var guildUser = await taylorBotClient.Value.ResolveGuildUserAsync(guild.Id, userId);
        ArgumentNullException.ThrowIfNull(guildUser);

        var embed = new EmbedBuilder()
            .WithGuildAsAuthor(guild)
            .WithColor(ModMailMessageUserSlashCommand.EmbedColor)
            .WithTitle("Message from the moderation team")
            .WithDescription(messageContent)
            .WithFooter("Reply with /modmail message-mods")
        .Build();

        await responseClient.EditOriginalResponseAsync(
            submit,
            message: new(new([embed, EmbedFactory.CreateWarning($"Are you sure you want to send the above message to {guildUser.FormatTagAndMention()}?")]),
            [
                new("mmmrmy", Style: ButtonStyle.Success, Label: "Confirm"),
                new(GenericPromptCancelButtonHandler.CustomIdName, Style: ButtonStyle.Danger, Label: "Cancel"),
            ],
            IsPrivate: false));
    }

    [GeneratedRegex(@"<@(\d+)>")]
    private static partial Regex UserIdRegex();
}
