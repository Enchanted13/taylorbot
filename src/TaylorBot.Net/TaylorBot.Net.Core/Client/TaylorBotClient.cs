﻿using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TaylorBot.Net.Core.Configuration;
using TaylorBot.Net.Core.Logging;
using TaylorBot.Net.Core.Snowflake;

namespace TaylorBot.Net.Core.Client
{
    public class TaylorBotClient
    {
        private readonly ILogger<TaylorBotClient> logger;
        private readonly ILogSeverityToLogLevelMapper logSeverityToLogLevelMapper;
        private readonly TaylorBotToken taylorBotToken;

        public DiscordShardedClient DiscordShardedClient { get; }

        public TaylorBotClient(ILogger<TaylorBotClient> logger, ILogSeverityToLogLevelMapper logSeverityToLogLevelMapper, TaylorBotToken taylorBotToken, DiscordShardedClient discordShardedClient)
        {
            this.logger = logger;
            this.logSeverityToLogLevelMapper = logSeverityToLogLevelMapper;
            this.taylorBotToken = taylorBotToken;
            DiscordShardedClient = discordShardedClient;

            DiscordShardedClient.Log += LogAsync;
        }

        public async Task StartAsync()
        {
            await DiscordShardedClient.LoginAsync(TokenType.Bot, taylorBotToken.Token);
            await DiscordShardedClient.StartAsync();
        }

        private Task LogAsync(LogMessage log)
        {
            logger.Log(logSeverityToLogLevelMapper.MapFrom(log.Severity), LogString.From(log.ToString(prependTimestamp: false)));
            return Task.CompletedTask;
        }

        public SocketGuild ResolveRequiredGuild(SnowflakeId id)
        {
            var guild = DiscordShardedClient.GetGuild(id.Id);
            if (guild == null)
            {
                throw new InvalidOperationException($"Could not resolve Guild ID {id}.");
            }
            return guild;
        }
    }
}
