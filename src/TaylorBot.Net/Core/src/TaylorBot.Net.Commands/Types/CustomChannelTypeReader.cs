﻿using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace TaylorBot.Net.Commands.Types
{
    public class CustomChannelTypeReader<T> : TypeReader
        where T : class, IChannel
    {
        private class ChannelVal<U> where U : class, IChannel
        {
            public U Channel { get; }
            public float Score { get; }

            public ChannelVal(U channel, float score)
            {
                Channel = channel;
                Score = score;
            }
        }

        private void AddResultIfTypeMatches(Dictionary<ulong, ChannelVal<T>> results, IChannel channel, float score)
        {
            if (channel is T casted && !results.ContainsKey(channel.Id))
            {
                results.Add(casted.Id, new ChannelVal<T>(
                    casted,
                    score
                ));
            }
        }

        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var results = new Dictionary<ulong, ChannelVal<T>>();
            var channels = context.Guild != null && context.User is IGuildUser user ?
                (await context.Guild.GetChannelsAsync(CacheMode.CacheOnly)).Where(c => user.GetPermissions(c).Has(ChannelPermission.ViewChannel)).ToList() :
                (IReadOnlyCollection<IChannel>)new[] { context.Channel };

            // By Mention (1.0)
            if (MentionUtils.TryParseChannel(input, out var id))
                AddResultIfTypeMatches(results, channels.Single(c => c.Id == id), 1.00f);

            // By Id (0.9)
            if (ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                AddResultIfTypeMatches(results, channels.Single(c => c.Id == id), 0.90f);

            // By Name (0.6-0.8)
            foreach (var channel in channels)
            {
                if (channel.Name.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (string.Equals(input, channel.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        AddResultIfTypeMatches(results, channel, channel.Name == input ? 0.80f : 0.70f);
                    }
                    else
                    {
                        AddResultIfTypeMatches(results, channel, 0.60f);
                    }
                }
            }

            if (results.Count > 0)
            {
                return TypeReaderResult.FromSuccess(
                    results.Values.Select(c => new TypeReaderValue(c.Channel, c.Score)).ToImmutableArray()
                );
            }
            else
            {
                return TypeReaderResult.FromError(
                    CommandError.ObjectNotFound, $"Could not find channel '{input}'."
                );
            }
        }
    }
}