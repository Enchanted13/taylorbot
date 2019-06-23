﻿using Discord.WebSocket;
using System.Threading.Tasks;
using TaylorBot.Net.Core.Program.Events;
using TaylorBot.Net.Core.Tasks;
using TaylorBot.Net.MemberLogging.Domain;

namespace TaylorBot.Net.UserNotifier.Program.Events
{
    public class GuildUserLeftHandler : IGuildUserLeftHandler
    {
        private readonly TaskExceptionLogger taskExceptionLogger;
        private readonly GuildMemberLeftLoggerService guildMemberLeftLoggerService;

        public GuildUserLeftHandler(TaskExceptionLogger taskExceptionLogger, GuildMemberLeftLoggerService guildMemberLeftLoggerService)
        {
            this.taskExceptionLogger = taskExceptionLogger;
            this.guildMemberLeftLoggerService = guildMemberLeftLoggerService;
        }

        public Task GuildUserLeftAsync(SocketGuildUser guildUser)
        {
            Task.Run(async () => await taskExceptionLogger.LogOnError(
                guildMemberLeftLoggerService.OnGuildMemberLeftAsync(guildUser), nameof(guildMemberLeftLoggerService.OnGuildMemberLeftAsync)
            ));
            return Task.CompletedTask;
        }
    }
}
