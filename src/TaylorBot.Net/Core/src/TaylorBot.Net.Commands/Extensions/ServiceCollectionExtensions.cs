﻿using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaylorBot.Net.Commands.DiscordNet;
using TaylorBot.Net.Commands.Events;
using TaylorBot.Net.Commands.Parsers;
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
                .AddTransient<IInteractionCreatedHandler, SlashCommandHandler>()
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
                .AddOptionParser<ParsedString, StringParser>()
                .AddOptionParser<ParsedOptionalString, OptionalStringParser>()
                .AddOptionParser<ParsedUserOrAuthor, UserOrAuthorParser>()
                .AddOptionParser<ParsedMember, MemberParser>()
                .AddOptionParser<ParsedMemberNotAuthor, MemberNotAuthorParser>()
                .AddOptionParser<ParsedMemberNotAuthorAndTaylorBot, ParsedMemberNotAuthorAndTaylorBotParser>()
                .AddOptionParser<ParsedTextChannelOrCurrent, TextChannelOrCurrentParser>();
        }

        public static IServiceCollection AddOptionParser<U, T>(this IServiceCollection services)
            where T : class, IOptionParser<U>
            where U : IParseResult
        {
            return services
                .AddTransient<T>()
                .AddTransient<IOptionParser, T>();
        }

        public static IServiceCollection AddSlashCommand<T, U>(this IServiceCollection services)
            where T : class, ISlashCommand<U>
        {
            return services.AddTransient<ISlashCommand, T>();
        }
    }
}
