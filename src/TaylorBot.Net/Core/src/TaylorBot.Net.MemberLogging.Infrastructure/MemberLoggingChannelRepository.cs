﻿using Dapper;
using Discord;
using TaylorBot.Net.Core.Infrastructure;
using TaylorBot.Net.Core.Snowflake;
using TaylorBot.Net.MemberLogging.Domain.TextChannel;

namespace TaylorBot.Net.MemberLogging.Infrastructure
{
    public class MemberLoggingChannelRepository : IMemberLoggingChannelRepository
    {
        private readonly PostgresConnectionFactory _postgresConnectionFactory;

        public MemberLoggingChannelRepository(PostgresConnectionFactory postgresConnectionFactory)
        {
            _postgresConnectionFactory = postgresConnectionFactory;
        }

        private class LogChannelDto
        {
            public string member_log_channel_id { get; set; } = null!;
        }

        public async ValueTask<LogChannel?> GetLogChannelForGuildAsync(IGuild guild)
        {
            await using var connection = _postgresConnectionFactory.CreateConnection();

            var logChannel = await connection.QuerySingleOrDefaultAsync<LogChannelDto?>(
                @"SELECT member_log_channel_id FROM plus.member_log_channels
                WHERE guild_id = @GuildId AND EXISTS (
                    SELECT FROM plus.plus_guilds
                    WHERE state = 'enabled' AND plus.member_log_channels.guild_id = plus.plus_guilds.guild_id
                );",
                new
                {
                    GuildId = guild.Id.ToString()
                }
            );

            return logChannel != null ? new LogChannel(new SnowflakeId(logChannel.member_log_channel_id)) : null;
        }
    }
}
