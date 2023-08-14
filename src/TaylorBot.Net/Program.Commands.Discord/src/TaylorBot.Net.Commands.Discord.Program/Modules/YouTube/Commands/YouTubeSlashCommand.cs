﻿using System;
using System.Threading.Tasks;
using TaylorBot.Net.Commands.Discord.Program.Modules.YouTube.Domain;
using TaylorBot.Net.Commands.PageMessages;
using TaylorBot.Net.Commands.Parsers;
using TaylorBot.Net.Commands.PostExecution;
using TaylorBot.Net.Core.Embed;

namespace TaylorBot.Net.Commands.Discord.Program.Modules.YouTube.Commands;

public class YouTubeSlashCommand : ISlashCommand<YouTubeSlashCommand.Options>
{
    public ISlashCommandInfo Info => new MessageCommandInfo("youtube");

    public record Options(ParsedString search);

    private readonly IYouTubeClient _youTubeClient;
    private readonly IRateLimiter _rateLimiter;

    public YouTubeSlashCommand(IYouTubeClient youTubeClient, IRateLimiter rateLimiter)
    {
        _youTubeClient = youTubeClient;
        _rateLimiter = rateLimiter;
    }

    public ValueTask<Command> GetCommandAsync(RunContext context, Options options)
    {
        return new(new Command(
            new(Info.Name),
            async () =>
            {
                var rateLimitResult = await _rateLimiter.VerifyDailyLimitAsync(context.User, "youtube-search");
                if (rateLimitResult != null)
                    return rateLimitResult;

                var result = await _youTubeClient.SearchAsync(options.search.Value);

                return result switch
                {
                    SuccessfulSearch search => search.VideoUrls.Count > 0
                        ? new PageMessageResultBuilder(new(
                            new(new MessageTextEditor(search.VideoUrls, emptyText: "No YouTube video found for your search 😕")),
                            IsCancellable: true
                        )).Build()
                        : new EmbedResult(EmbedFactory.CreateError("No YouTube video found for your search 😕")),

                    GenericError error => new EmbedResult(EmbedFactory.CreateError(
                        """
                        YouTube returned an unexpected error. 😢
                        The site might be down. Try again later!
                        """
                    )),

                    _ => throw new InvalidOperationException(result.GetType().Name),
                };
            }
        ));
    }
}
