﻿using Discord.WebSocket;
using System.Threading.Tasks;
using TaylorBot.Net.Core.Program.Events;
using TaylorBot.Net.MessageLogging.Domain;

namespace TaylorBot.Net.UserNotifier.Program.Events
{
    public class MessageReceivedHandler : IMessageReceivedHandler
    {
        private readonly MessageDeletedLoggerService _messageDeletedLoggerService;

        public MessageReceivedHandler(MessageDeletedLoggerService messageDeletedLoggerService)
        {
            _messageDeletedLoggerService = messageDeletedLoggerService;
        }

        public async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Channel is SocketTextChannel textChannel)
            {
                await _messageDeletedLoggerService.OnGuildUserMessageReceivedAsync(textChannel, message);
            }
        }
    }
}