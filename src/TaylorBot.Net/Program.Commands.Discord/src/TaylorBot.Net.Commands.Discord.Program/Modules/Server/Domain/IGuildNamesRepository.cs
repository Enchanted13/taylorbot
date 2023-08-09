﻿using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaylorBot.Net.Commands.Discord.Program.Modules.Server.Domain;

public record GuildNameEntry(string GuildName, DateTimeOffset ChangedAt);

public interface IGuildNamesRepository
{
    ValueTask<List<GuildNameEntry>> GetHistoryAsync(IGuild guild, int limit);
}
