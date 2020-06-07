﻿using Discord;
using System;
using System.Reflection;

namespace TaylorBot.Net.Commands.Discord.Program.Tests
{
    public class PresenceFactory
    {
        public CustomStatusGame CreateCustomStatus(string status)
        {
            var type = typeof(CustomStatusGame);
            var instance = (CustomStatusGame)type.Assembly.CreateInstance(
                type.FullName, ignoreCase: false,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, Array.Empty<object>(), null, null
            );

            type.GetProperty(nameof(CustomStatusGame.State))
                .SetValue(instance, status);

            return instance;
        }
    }
}