﻿using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using TaylorBot.Net.Commands.DiscordNet;
using TaylorBot.Net.Commands.Events;
using TaylorBot.Net.Commands.Parsers;
using TaylorBot.Net.Commands.Parsers.Numbers;
using TaylorBot.Net.Commands.Parsers.Users;
using TaylorBot.Net.Commands.PostExecution;
using TaylorBot.Net.Commands.Preconditions;
using TaylorBot.Net.Commands.Types;
using TaylorBot.Net.Core.Program.Events;
using TaylorBot.Net.Core.Program.Extensions;
using TaylorBot.Net.Core.Tasks;

namespace TaylorBot.Net.Commands.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCommandApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<InteractionResponseClient>((provider, client) =>
            {
                client.BaseAddress = new Uri("https://discord.com/api/v9/");
            });

            return services
                .AddTaylorBotApplicationServices(configuration)
                .AddSingleton(services)
                .AddSingleton(provider => new CommandService(new CommandServiceConfig
                {
                    DefaultRunMode = RunMode.Async
                }))
                .AddTransient<SingletonTaskRunner>()
                .AddTransient<IUserMessageReceivedHandler, CommandHandler>()
                .AddTransient<IAllReadyHandler, CommandHandler>()
                .AddTransient<SlashCommandHandler>()
                .AddSingleton<MessageComponentHandler>()
                .AddTransient<IInteractionCreatedHandler, InteractionCreatedHandler>()
                .AddSingleton<PageMessageReactionsHandler>()
                .AddTransient<IReactionAddedHandler>(c => c.GetRequiredService<PageMessageReactionsHandler>())
                .AddTransient<IReactionRemovedHandler>(c => c.GetRequiredService<PageMessageReactionsHandler>())
                .AddTransient<CommandExecutedHandler>()
                .AddTransient<CommandServiceLogger>()
                .AddTransient<IRateLimiter, RateLimiter>()
                .AddTransient<IUserTracker, UserTracker>()
                .AddTransient<MentionedUserTypeReader<IUser>>()
                .AddTransient<CustomUserTypeReader<IUser>>()
                .AddTransient<MentionedUserTypeReader<IGuildUser>>()
                .AddTransient<CustomUserTypeReader<IGuildUser>>()
                .AddTransient<MentionedUserNotAuthorTypeReader<IGuildUser>>()
                .AddTransient<MentionedUserNotAuthorTypeReader<IUser>>()
                .AddTransient<MentionedUsersNotAuthorTypeReader<IUser>>()
                .AddTransient<MentionedUserNotAuthorOrClientTypeReader<IGuildUser>>()
                .AddTransient<CustomRoleTypeReader<IRole>>()
                .AddTransient<RoleNotEveryoneTypeReader<IRole>>()
                .AddTransient<CustomChannelTypeReader<IChannel>>()
                .AddTransient<CustomChannelTypeReader<ITextChannel>>()
                .AddTransient<PositiveInt32.Factory>()
                .AddTransient<WordTypeReader>()
                .AddTransient<CommandTypeReader>()
                .AddTransient<NotDisabledPrecondition>()
                .AddTransient<NotGuildDisabledPrecondition>()
                .AddTransient<NotGuildChannelDisabledPrecondition>()
                .AddTransient<UserNotIgnoredPrecondition>()
                .AddTransient<MemberTrackedPrecondition>()
                .AddTransient<TextChannelTrackedPrecondition>()
                .AddTransient<UserNoOngoingCommandPrecondition>()
                .AddTransient<ICommandRunner, CommandRunner>()
                .AddOptionParser<StringParser>()
                .AddOptionParser<OptionalStringParser>()
                .AddOptionParser<UserParser>()
                .AddOptionParser<OptionalUserParser>()
                .AddOptionParser<UserNotAuthorParser>()
                .AddOptionParser<UserNotAuthorAndTaylorBotParser>()
                .AddOptionParser<UserNotAuthorAndBotParser>()
                .AddOptionParser<UserOrAuthorParser>()
                .AddOptionParser<MemberParser>()
                .AddOptionParser<MemberNotAuthorParser>()
                .AddOptionParser<MemberNotAuthorAndTaylorBotParser>()
                .AddOptionParser<MemberNotAuthorAndBotParser>()
                .AddOptionParser<TextChannelOrCurrentParser>()
                .AddOptionParser<TimeSpanParser>()
                .AddOptionParser<IntegerParser>()
                .AddOptionParser<PositiveIntegerParser>();
        }

        public static IServiceCollection AddOptionParser<T>(this IServiceCollection services)
            where T : class, IOptionParser
        {
            return services
                .AddTransient<T>()
                .AddTransient<IOptionParser, T>();
        }

        public static IServiceCollection AddSlashCommand<T>(this IServiceCollection services)
            where T : class, ISlashCommand
        {
            return services.AddTransient<ISlashCommand, T>();
        }
    }
}
