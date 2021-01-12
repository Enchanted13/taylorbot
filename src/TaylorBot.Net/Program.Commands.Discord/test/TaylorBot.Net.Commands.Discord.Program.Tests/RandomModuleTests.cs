﻿using Discord;
using FakeItEasy;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaylorBot.Net.Commands.Discord.Program.Modules;
using TaylorBot.Net.Commands.PostExecution;
using TaylorBot.Net.Commands.Types;
using Xunit;

namespace TaylorBot.Net.Commands.Discord.Program.Tests
{
    public class RandomModuleTests
    {
        private readonly IUser _commandUser = A.Fake<IUser>();
        private readonly ITaylorBotCommandContext _commandContext = A.Fake<ITaylorBotCommandContext>(o => o.Strict());
        private readonly ICryptoSecureRandom _cryptoSecureRandom = A.Fake<ICryptoSecureRandom>(o => o.Strict());
        private readonly RandomModule _randomModule;

        public RandomModuleTests()
        {
            _randomModule = new RandomModule(_cryptoSecureRandom);
            _randomModule.SetContext(_commandContext);
            A.CallTo(() => _commandContext.User).Returns(_commandUser);
        }

        [Fact]
        public async Task DiceAsync_ThenReturnsEmbedWithRoll()
        {
            const int FaceCount = 6;
            const int Roll = 2;
            A.CallTo(() => _cryptoSecureRandom.GetRandomInt32(0, FaceCount)).Returns(Roll - 1);

            var result = (TaylorBotEmbedResult)await _randomModule.DiceAsync(new PositiveInt32(FaceCount));

            result.Embed.Description.Should().Contain(Roll.ToString());
        }

        [Fact]
        public async Task ChooseAsync_ThenReturnsEmbedWithChosenOption()
        {
            const string ChosenOption = "Speak Now";
            A.CallTo(() => _cryptoSecureRandom.GetRandomElement(A<IReadOnlyList<string>>.That.Contains(ChosenOption))).Returns(ChosenOption);

            var result = (TaylorBotEmbedResult)await _randomModule.ChooseAsync($"Taylor Swift, Fearless, {ChosenOption}, Red, 1989, reputation, Lover");

            result.Embed.Description.Should().Be(ChosenOption);
        }
    }
}
