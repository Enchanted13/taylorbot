﻿using Discord;
using FakeItEasy;
using FluentAssertions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace TaylorBot.Net.Commands.Types.Tests
{
    public class MentionedUserTypeReaderTests
    {
        private const ulong AnId = 1;
        private static readonly IUser AUser = A.Fake<IUser>();
        private static readonly IGuildUser AGuildUser = A.Fake<IGuildUser>();

        private readonly ITaylorBotCommandContext _commandContext = A.Fake<ITaylorBotCommandContext>(o => o.Strict());
        private readonly IServiceProvider _serviceProvider = A.Fake<IServiceProvider>(o => o.Strict());
        private readonly IUserTracker _userTracker = A.Fake<IUserTracker>(o => o.Strict());

        [Fact]
        public async Task ReadAsync_WhenIUserMentionInChannel_ThenReturnsUser()
        {
            var mentionedUserTypeReader = new MentionedUserTypeReader<IUser>(_userTracker);
            var channel = A.Fake<IMessageChannel>(o => o.Strict());
            A.CallTo(() => _commandContext.Guild).Returns(null);
            A.CallTo(() => _commandContext.Channel).Returns(channel);
            A.CallTo(() => channel.GetUserAsync(AnId, CacheMode.AllowDownload, null)).Returns(AUser);
            A.CallTo(() => _userTracker.TrackUserFromArgumentAsync(AUser)).Returns(default);

            var result = (IMentionedUser<IUser>)(await mentionedUserTypeReader.ReadAsync(_commandContext, MentionUtils.MentionUser(AnId), _serviceProvider)).Values.Single().Value;
            var user = await result.GetTrackedUserAsync();

            user.Should().Be(AUser);
        }

        [Fact]
        public async Task ReadAsync_WhenIGuildUserMentionInGuild_ThenReturnsIGuildUser()
        {
            var mentionedUserTypeReader = new MentionedUserTypeReader<IGuildUser>(_userTracker);
            var guild = A.Fake<IGuild>(o => o.Strict());
            A.CallTo(() => _commandContext.Guild).Returns(guild);
            A.CallTo(() => guild.GetUserAsync(AnId, CacheMode.AllowDownload, null)).Returns(AGuildUser);
            A.CallTo(() => _userTracker.TrackUserFromArgumentAsync(AGuildUser)).Returns(default);

            var result = (IMentionedUser<IGuildUser>)(await mentionedUserTypeReader.ReadAsync(_commandContext, MentionUtils.MentionUser(AnId), _serviceProvider)).Values.Single().Value;
            var user = await result.GetTrackedUserAsync();

            user.Should().Be(AGuildUser);
        }
    }
}