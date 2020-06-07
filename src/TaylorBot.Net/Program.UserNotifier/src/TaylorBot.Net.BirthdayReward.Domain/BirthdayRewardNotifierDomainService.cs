﻿using Discord;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using TaylorBot.Net.BirthdayReward.Domain.DiscordEmbed;
using TaylorBot.Net.BirthdayReward.Domain.Options;
using TaylorBot.Net.Core.Client;
using TaylorBot.Net.Core.Logging;

namespace TaylorBot.Net.BirthdayReward.Domain
{
    public class BirthdayRewardNotifierDomainService
    {
        private readonly ILogger<BirthdayRewardNotifierDomainService> _logger;
        private readonly IOptionsMonitor<BirthdayRewardNotifierOptions> _optionsMonitor;
        private readonly IBirthdayRepository _birthdayRepository;
        private readonly BirthdayRewardEmbedFactory _birthdayRewardEmbedFactory;
        private readonly ITaylorBotClient _taylorBotClient;

        public BirthdayRewardNotifierDomainService(
            ILogger<BirthdayRewardNotifierDomainService> logger,
            IOptionsMonitor<BirthdayRewardNotifierOptions> optionsMonitor,
            IBirthdayRepository birthdayRepository,
            BirthdayRewardEmbedFactory birthdayRewardEmbedFactory,
            ITaylorBotClient taylorBotClient)
        {
            _logger = logger;
            _optionsMonitor = optionsMonitor;
            _birthdayRewardEmbedFactory = birthdayRewardEmbedFactory;
            _birthdayRepository = birthdayRepository;
            _taylorBotClient = taylorBotClient;
        }

        public async Task StartCheckingBirthdaysAsync()
        {
            while (true)
            {
                try
                {
                    await RewardBirthdaysAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, LogString.From($"Unhandled exception in {nameof(RewardBirthdaysAsync)}."));
                    await Task.Delay(_optionsMonitor.CurrentValue.TimeSpanBetweenMessages);
                    continue;
                }
                await Task.Delay(_optionsMonitor.CurrentValue.TimeSpanBetweenRewards);
            }
        }

        public async ValueTask RewardBirthdaysAsync()
        {
            var rewardAmount = _optionsMonitor.CurrentValue.RewardAmount;
            _logger.LogTrace(LogString.From($"Rewarding eligible users with {"birthday point".ToQuantity(rewardAmount)}."));

            foreach (var rewardedUser in await _birthdayRepository.RewardEligibleUsersAsync(rewardAmount))
            {
                try
                {
                    _logger.LogTrace(LogString.From($"Rewarded {"birthday point".ToQuantity(rewardAmount)} to {rewardedUser}."));
                    var user = await _taylorBotClient.ResolveRequiredUserAsync(rewardedUser.UserId);
                    await user.SendMessageAsync(embed: _birthdayRewardEmbedFactory.Create(rewardAmount, rewardedUser));
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, LogString.From($"Exception occurred when attempting to notify {rewardedUser} about their birthday reward."));
                }

                await Task.Delay(_optionsMonitor.CurrentValue.TimeSpanBetweenMessages);
            }
        }
    }
}