﻿using Discord;
using System.Collections.Concurrent;
using TaylorBot.Net.Commands.Preconditions;

namespace TaylorBot.Net.Commands.Infrastructure
{
    public class OnGoingCommandInMemoryRepository : IOngoingCommandRepository
    {
        private readonly IDictionary<string, long> ongoingCommands = new ConcurrentDictionary<string, long>();

        private string GetKey(IUser user, string pool) => $"{user.Id}{pool}";

        public ValueTask AddOngoingCommandAsync(IUser user, string pool)
        {
            var key = GetKey(user, pool);
            if (ongoingCommands.TryGetValue(key, out var count))
            {
                ongoingCommands.Remove(key);
                ongoingCommands.Add(key, count + 1);
            }
            else
            {
                ongoingCommands.Add(key, 1);
            }

            return default;
        }

        public ValueTask<bool> HasAnyOngoingCommandAsync(IUser user, string pool)
        {
            return new ValueTask<bool>(
                ongoingCommands.TryGetValue(GetKey(user, pool), out var count) && count > 0
            );
        }

        public ValueTask RemoveOngoingCommandAsync(IUser user, string pool)
        {
            var key = GetKey(user, pool);
            if (ongoingCommands.TryGetValue(key, out var count))
            {
                ongoingCommands.Remove(key);
                ongoingCommands.Add(key, count - 1);
            }

            return default;
        }
    }
}
