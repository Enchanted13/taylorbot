﻿using Discord;
using StackExchange.Redis;
using TaylorBot.Net.Commands.Preconditions;

namespace TaylorBot.Net.Commands.Infrastructure
{
    public class OnGoingCommandRedisRepository : IOngoingCommandRepository
    {
        private readonly ConnectionMultiplexer _connectionMultiplexer;

        public OnGoingCommandRedisRepository(ConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
        }

        private string GetKey(IUser user, string pool) => $"ongoing-commands:user:{user.Id}{pool}";

        public async ValueTask AddOngoingCommandAsync(IUser user, string pool)
        {
            var redis = _connectionMultiplexer.GetDatabase();
            var key = GetKey(user, pool);
            await redis.StringSetAsync(key, "1", expiry: TimeSpan.FromSeconds(10));
        }

        public async ValueTask<bool> HasAnyOngoingCommandAsync(IUser user, string pool)
        {
            var redis = _connectionMultiplexer.GetDatabase();
            var key = GetKey(user, pool);
            var ongoingCommands = await redis.StringGetAsync(key);

            return ongoingCommands.HasValue && (long)ongoingCommands > 0;
        }

        public async ValueTask RemoveOngoingCommandAsync(IUser user, string pool)
        {
            var redis = _connectionMultiplexer.GetDatabase();
            var key = GetKey(user, pool);
            await redis.StringSetAsync(key, "0");
        }
    }
}
