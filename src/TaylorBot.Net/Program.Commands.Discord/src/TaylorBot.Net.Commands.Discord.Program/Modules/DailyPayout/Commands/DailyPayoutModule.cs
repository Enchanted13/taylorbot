﻿using Discord.Commands;
using TaylorBot.Net.Commands.DiscordNet;
using TaylorBot.Net.Core.Embed;

namespace TaylorBot.Net.Commands.Discord.Program.Modules.DailyPayout.Commands;

[Name("Daily Payout 👔")]
public class DailyPayoutModule : TaylorBotModule
{
    private readonly ICommandRunner _commandRunner;
    private readonly DailyClaimCommand _dailyClaimCommand;

    public DailyPayoutModule(ICommandRunner commandRunner, DailyClaimCommand dailyClaimCommand)
    {
        _commandRunner = commandRunner;
        _dailyClaimCommand = dailyClaimCommand;
    }

    [Command("daily")]
    [Alias("dailypayout")]
    [Summary("Awards you with your daily amount of taypoints.")]
    public async Task<RuntimeResult> DailyAsync()
    {
        var context = DiscordNetContextMapper.MapToRunContext(Context);
        var result = await _commandRunner.RunAsync(
            _dailyClaimCommand.Claim(Context.User, Context.CommandPrefix, isLegacyCommand: true),
            context
        );

        return new TaylorBotResult(result, context);
    }

    [Command("dailystreak")]
    [Alias("dstreak")]
    [Summary("This command has been moved to </daily streak:870731803739168859>. Please use it instead! 😊")]
    public async Task<RuntimeResult> DailyStreakAsync(
        [Remainder]
        string? _ = null
    )
    {
        var command = new Command(
            DiscordNetContextMapper.MapToCommandMetadata(Context),
            () => new(new EmbedResult(EmbedFactory.CreateError(
                """
                This command has been moved to 👉 </daily streak:870731803739168859> 👈
                Please use it instead! 😊
                """))));

        var context = DiscordNetContextMapper.MapToRunContext(Context);
        var result = await _commandRunner.RunAsync(command, context);

        return new TaylorBotResult(result, context);
    }

    [Command("rankdailystreak")]
    [Alias("rank dailystreak", "rankdstreak", "rank dstreak")]
    [Summary("This command has been moved to </daily leaderboard:870731803739168859>. Please use it instead! 😊")]
    public async Task<RuntimeResult> RankDailyStreakAsync(
        [Remainder]
        string? _ = null
    )
    {
        var command = new Command(
            DiscordNetContextMapper.MapToCommandMetadata(Context),
            () => new(new EmbedResult(EmbedFactory.CreateError(
                """
                This command has been moved to 👉 </daily leaderboard:870731803739168859> 👈
                Please use it instead! 😊
                """))));

        var context = DiscordNetContextMapper.MapToRunContext(Context);
        var result = await _commandRunner.RunAsync(command, context);

        return new TaylorBotResult(result, context);
    }
}
