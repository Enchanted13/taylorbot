﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using TaylorBot.Net.BirthdayReward.Domain;
using TaylorBot.Net.BirthdayReward.Domain.DiscordEmbed;
using TaylorBot.Net.BirthdayReward.Domain.Options;
using TaylorBot.Net.BirthdayReward.Infrastructure;
using TaylorBot.Net.Core.Configuration;
using TaylorBot.Net.Core.Infrastructure.Configuration;
using TaylorBot.Net.Core.Program;
using TaylorBot.Net.Core.Program.Events;
using TaylorBot.Net.Core.Program.Extensions;
using TaylorBot.Net.Core.Tasks;
using TaylorBot.Net.MemberLogging.Domain;
using TaylorBot.Net.MemberLogging.Domain.DiscordEmbed;
using TaylorBot.Net.MemberLogging.Domain.Options;
using TaylorBot.Net.MemberLogging.Domain.TextChannel;
using TaylorBot.Net.MemberLogging.Infrastructure;
using TaylorBot.Net.MessageLogging.Domain;
using TaylorBot.Net.MessageLogging.Domain.DiscordEmbed;
using TaylorBot.Net.MessageLogging.Domain.Options;
using TaylorBot.Net.MessageLogging.Domain.TextChannel;
using TaylorBot.Net.MessageLogging.Infrastructure;
using TaylorBot.Net.Reminder.Domain;
using TaylorBot.Net.Reminder.Domain.DiscordEmbed;
using TaylorBot.Net.Reminder.Domain.Options;
using TaylorBot.Net.Reminder.Infrastructure;
using TaylorBot.Net.UserNotifier.Program.Events;

namespace TaylorBot.Net.UserNotifier.Program
{
    public class UserNotifierProgram
    {
        public static async Task Main()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostBuilderContext, appConfig) =>
                {
                    appConfig.Sources.Clear();

                    var env = hostBuilderContext.HostingEnvironment;

                    appConfig
                        .AddTaylorBotApplication(env)
                        .AddDatabaseConnection(env)
                        .AddJsonFile(path: "Settings/birthdayRewardNotifier.json", optional: false, reloadOnChange: true)
                        .AddJsonFile(path: "Settings/reminderNotifier.json", optional: false, reloadOnChange: true)
                        .AddJsonFile(path: "Settings/memberLog.json", optional: false, reloadOnChange: true)
                        .AddJsonFile(path: "Settings/messageLog.json", optional: false, reloadOnChange: true);

                    appConfig.AddEnvironmentVariables("TaylorBot_");
                })
                .ConfigureServices((hostBuilderContext, services) =>
                {
                    var config = hostBuilderContext.Configuration;
                    services
                        .AddHostedService<TaylorBotHostedService>()
                        .AddTaylorBotApplicationServices(config)
                        .AddPostgresConnection(config)
                        .ConfigureRequired<BirthdayRewardNotifierOptions>(config, "BirthdayRewardNotifier")
                        .ConfigureRequired<ReminderNotifierOptions>(config, "ReminderNotifier")
                        .ConfigureRequired<MemberLeftLoggingOptions>(config, "MemberLeft")
                        .ConfigureRequired<MemberBanLoggingOptions>(config, "MemberBan")
                        .ConfigureRequired<MessageDeletedLoggingOptions>(config, "MessageDeleted")
                        .AddTransient<IAllReadyHandler, ReadyHandler>()
                        .AddTransient<IGuildUserLeftHandler, GuildUserLeftHandler>()
                        .AddTransient<IGuildUserBannedHandler, GuildUserBanHandler>()
                        .AddTransient<IGuildUserUnbannedHandler, GuildUserBanHandler>()
                        .AddTransient<IMessageDeletedHandler, MessageDeletedHandler>()
                        .AddTransient<SingletonTaskRunner>()
                        .AddTransient<IBirthdayRepository, BirthdayRepository>()
                        .AddTransient<BirthdayRewardNotifierDomainService>()
                        .AddTransient<BirthdayRewardEmbedFactory>()
                        .AddTransient<IReminderRepository, ReminderRepository>()
                        .AddTransient<ReminderNotifierDomainService>()
                        .AddTransient<ReminderEmbedFactory>()
                        .AddTransient<IMemberLoggingChannelRepository, MemberLoggingChannelRepository>()
                        .AddTransient<MemberLogChannelFinder>()
                        .AddTransient<GuildMemberLeftEmbedFactory>()
                        .AddTransient<GuildMemberLeftLoggerService>()
                        .AddTransient<GuildMemberBanEmbedFactory>()
                        .AddTransient<GuildMemberBanLoggerService>()
                        .AddTransient<IMessageLoggingChannelRepository, MessageLoggingChannelRepository>()
                        .AddTransient<MessageLogChannelFinder>()
                        .AddTransient<MessageDeletedEmbedFactory>()
                        .AddTransient<MessageDeletedLoggerService>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}