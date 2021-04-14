﻿using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaylorBot.Net.Commands.DiscordNet;
using TaylorBot.Net.Commands.Preconditions;
using TaylorBot.Net.Core.Colors;
using TaylorBot.Net.Core.Embed;

namespace TaylorBot.Net.Commands.Discord.Program.Modules.CommandDisabling
{
    [Name("Command")]
    [Group("command")]
    [Alias("c")]
    public class CommandModule : TaylorBotModule
    {
        private static readonly string[] GuardedModuleNames = new[] { "framework", "command" };

        private readonly ICommandRunner _commandRunner;
        private readonly IDisabledCommandRepository _disabledCommandRepository;

        public CommandModule(ICommandRunner commandRunner, IDisabledCommandRepository disabledCommandRepository)
        {
            _commandRunner = commandRunner;
            _disabledCommandRepository = disabledCommandRepository;
        }

        [Command("enable-global")]
        [Summary("Enables a command globally.")]
        public async Task<RuntimeResult> EnableGlobalAsync(
            [Summary("What command would you like to enable?")]
            [Remainder]
            ICommandRepository.Command command
        )
        {
            var enableCommand = new Command(DiscordNetContextMapper.MapToCommandMetadata(Context), async () =>
            {
                await _disabledCommandRepository.EnableGloballyAsync(command.Name);

                return new EmbedResult(new EmbedBuilder()
                    .WithUserAsAuthor(Context.User)
                    .WithColor(TaylorBotColors.SuccessColor)
                    .WithDescription($"Command `{command.Name}` has been enabled globally.")
                .Build());
            });

            var context = DiscordNetContextMapper.MapToRunContext(Context);
            var result = await _commandRunner.RunAsync(enableCommand, context, new[] { new TaylorBotOwnerPrecondition() });

            return new TaylorBotResult(result, context);
        }

        [Command("disable-global")]
        [Summary("Disables a command globally.")]
        public async Task<RuntimeResult> DisableGlobalAsync(
            [Summary("What command would you like to disable?")]
            ICommandRepository.Command command,
            [Remainder]
            [Summary("What message should be shown to someone using the disabled command?")]
            string message
        )
        {
            var disableCommand = new Command(DiscordNetContextMapper.MapToCommandMetadata(Context), async () =>
            {
                var embed = new EmbedBuilder().WithUserAsAuthor(Context.User);

                if (GuardedModuleNames.Contains(command.ModuleName.ToLowerInvariant()))
                {
                    return new EmbedResult(embed
                        .WithColor(TaylorBotColors.ErrorColor)
                        .WithDescription($"Command `{command.Name}` can't be disabled because it's a framework command.")
                    .Build());
                }

                var disabledMessage = await _disabledCommandRepository.DisableGloballyAsync(command.Name, message);

                return new EmbedResult(embed
                    .WithColor(TaylorBotColors.SuccessColor)
                    .WithDescription($"Command `{command.Name}` has been disabled globally with message '{disabledMessage}'.")
                .Build());
            });

            var context = DiscordNetContextMapper.MapToRunContext(Context);
            var result = await _commandRunner.RunAsync(disableCommand, context, new[] { new TaylorBotOwnerPrecondition() });

            return new TaylorBotResult(result, context);
        }
    }
}