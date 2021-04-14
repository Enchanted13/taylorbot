﻿using Discord;
using Discord.Commands;
using Humanizer;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaylorBot.Net.Commands.Discord.Program.Modules.LastFm.Domain;
using TaylorBot.Net.Commands.Discord.Program.Options;
using TaylorBot.Net.Commands.DiscordNet;
using TaylorBot.Net.Commands.Types;
using TaylorBot.Net.Core.Colors;
using TaylorBot.Net.Core.Embed;
using TaylorBot.Net.Core.Number;
using TaylorBot.Net.Core.Strings;
using TaylorBot.Net.Core.User;

namespace TaylorBot.Net.Commands.Discord.Program.Modules.LastFm.Commands
{
    [Name("Last.fm 🎶")]
    [Group("lastfm")]
    [Alias("fm", "np")]
    public class LastFmModule : TaylorBotModule
    {
        private readonly ICommandRunner _commandRunner;
        private readonly IOptionsMonitor<LastFmOptions> _options;
        private readonly ILastFmUsernameRepository _lastFmUsernameRepository;
        private readonly ILastFmClient _lastFmClient;
        private readonly LastFmPeriodStringMapper _lastFmPeriodStringMapper;

        public LastFmModule(
            ICommandRunner commandRunner,
            IOptionsMonitor<LastFmOptions> options,
            ILastFmUsernameRepository lastFmUsernameRepository,
            ILastFmClient lastFmClient,
            LastFmPeriodStringMapper lastFmPeriodStringMapper
        )
        {
            _commandRunner = commandRunner;
            _options = options;
            _lastFmUsernameRepository = lastFmUsernameRepository;
            _lastFmClient = lastFmClient;
            _lastFmPeriodStringMapper = lastFmPeriodStringMapper;
        }

        [Priority(-1)]
        [Command]
        [Summary("Displays the currently playing or most recently played track for a user's Last.fm profile.")]
        public async Task<RuntimeResult> NowPlayingAsync(
            [Summary("What user would you like to see the now playing of?")]
            [Remainder]
            IUserArgument<IUser>? user = null
        )
        {
            var command = new Command(DiscordNetContextMapper.MapToCommandMetadata(Context), async () =>
            {
                var u = user == null ? Context.User : await user.GetTrackedUserAsync();

                var lastFmUsername = await _lastFmUsernameRepository.GetLastFmUsernameAsync(u);

                if (lastFmUsername == null)
                    return new EmbedResult(CreateLastFmNotSetEmbed(u));

                var result = await _lastFmClient.GetMostRecentScrobbleAsync(lastFmUsername.Username);

                switch (result)
                {
                    case MostRecentScrobbleResult success:
                        if (success.MostRecentTrack != null)
                        {
                            var embed = CreateBaseLastFmEmbed(lastFmUsername, u);

                            var mostRecentTrack = success.MostRecentTrack;

                            if (mostRecentTrack.TrackImageUrl != null)
                            {
                                embed.WithThumbnailUrl(mostRecentTrack.TrackImageUrl);
                            }

                            return new EmbedResult(embed
                                .WithColor(TaylorBotColors.SuccessColor)
                                .AddField("Artist", mostRecentTrack.Artist.Name.DiscordMdLink(mostRecentTrack.Artist.Url), inline: true)
                                .AddField("Track", mostRecentTrack.TrackName.DiscordMdLink(mostRecentTrack.TrackUrl), inline: true)
                                .WithFooter(text: string.Join(" | ", new[] {
                                    mostRecentTrack.IsNowPlaying ? "Now Playing" : "Most Recent Track",
                                    $"Total Scrobbles: {success.TotalScrobbles}"
                                }), iconUrl: _options.CurrentValue.LastFmEmbedFooterIconUrl)
                                .Build()
                            );
                        }
                        else
                        {
                            return new EmbedResult(
                                CreateLastFmNoScrobbleErrorEmbed(lastFmUsername, u, LastFmPeriod.Overall)
                            );
                        }

                    case LastFmLogInRequiredErrorResult _:
                        return new EmbedResult(new EmbedBuilder()
                            .WithUserAsAuthor(Context.User)
                            .WithColor(TaylorBotColors.ErrorColor)
                            .WithDescription(string.Join('\n', new[] {
                                "Last.fm says your recent tracks are not public. 😢",
                                $"Make sure 'Hide recent listening information' is off in your {"Last.fm privacy settings".DiscordMdLink("https://www.last.fm/settings/privacy")}!"
                            }))
                        .Build());

                    case LastFmGenericErrorResult errorResult:
                        return new EmbedResult(CreateLastFmErrorEmbed(errorResult));

                    default: throw new NotImplementedException();
                }
            });

            var context = DiscordNetContextMapper.MapToRunContext(Context);
            var result = await _commandRunner.RunAsync(command, context);

            return new TaylorBotResult(result, context);
        }

        [Command("set")]
        [Summary("Registers your Last.fm username for use with other Last.fm commands.")]
        public async Task<RuntimeResult> SetAsync(
            [Summary("What is your Last.fm username?")]
            LastFmUsername lastFmUsername
        )
        {
            var command = new Command(DiscordNetContextMapper.MapToCommandMetadata(Context), async () =>
            {
                await _lastFmUsernameRepository.SetLastFmUsernameAsync(Context.User, lastFmUsername);

                return new EmbedResult(new EmbedBuilder()
                    .WithUserAsAuthor(Context.User)
                    .WithColor(TaylorBotColors.SuccessColor)
                    .WithDescription(string.Join('\n', new[] {
                        $"Your Last.fm username has been set to {lastFmUsername.Username.DiscordMdLink(lastFmUsername.LinkToProfile)}. ✅",
                        $"You can now use Last.fm commands, get started with `{Context.CommandPrefix}lastfm`."
                    }))
                .Build());
            });

            var context = DiscordNetContextMapper.MapToRunContext(Context);
            var result = await _commandRunner.RunAsync(command, context);

            return new TaylorBotResult(result, context);
        }

        [Command("clear")]
        [Summary("Clears your registered Last.fm username.")]
        public async Task<RuntimeResult> ClearAsync()
        {
            var command = new Command(DiscordNetContextMapper.MapToCommandMetadata(Context), async () =>
            {
                await _lastFmUsernameRepository.ClearLastFmUsernameAsync(Context.User);

                return new EmbedResult(new EmbedBuilder()
                    .WithUserAsAuthor(Context.User)
                    .WithColor(TaylorBotColors.SuccessColor)
                    .WithDescription(string.Join('\n', new[] {
                        $"Your Last.fm username has been cleared. Last.fm commands will no longer work. ✅",
                        $"You can set it again with `{Context.CommandPrefix}lastfm set <username>`."
                    }))
                .Build());
            });

            var context = DiscordNetContextMapper.MapToRunContext(Context);
            var result = await _commandRunner.RunAsync(command, context);

            return new TaylorBotResult(result, context);
        }

        [Command("artists")]
        [Summary("Gets the top artists listened to by a user over a period.")]
        public async Task<RuntimeResult> ArtistsAsync(
            [Summary("What period of time would you like the top artists for?")]
            LastFmPeriod period = LastFmPeriod.SevenDay,
            [Summary("What user would you like to see a the top artists for?")]
            [Remainder]
            IUserArgument<IUser>? user = null
        )
        {
            var command = new Command(DiscordNetContextMapper.MapToCommandMetadata(Context), async () =>
            {
                var u = user == null ? Context.User : await user.GetTrackedUserAsync();

                var lastFmUsername = await _lastFmUsernameRepository.GetLastFmUsernameAsync(u);

                if (lastFmUsername == null)
                    return new EmbedResult(CreateLastFmNotSetEmbed(u));

                var result = await _lastFmClient.GetTopArtistsAsync(lastFmUsername.Username, period);

                switch (result)
                {
                    case LastFmGenericErrorResult errorResult:
                        return new EmbedResult(CreateLastFmErrorEmbed(errorResult));

                    case TopArtistsResult success:
                        if (success.TopArtists.Count > 0)
                        {
                            var formattedArtists = success.TopArtists.Select((a, index) =>
                                $"{index + 1}. {a.Name.DiscordMdLink(a.ArtistUrl.ToString())}: {"play".ToQuantity(a.PlayCount, TaylorBotFormats.BoldReadable)}"
                            );

                            return new EmbedResult(
                                CreateBaseLastFmEmbed(lastFmUsername, u)
                                    .WithColor(TaylorBotColors.SuccessColor)
                                    .WithTitle($"Top artists | {_lastFmPeriodStringMapper.MapLastFmPeriodToReadableString(period)}")
                                    .WithDescription(formattedArtists.CreateEmbedDescriptionWithMaxAmountOfLines())
                                    .Build()
                            );
                        }
                        else
                        {
                            return new EmbedResult(
                                CreateLastFmNoScrobbleErrorEmbed(lastFmUsername, u, period)
                            );
                        }

                    default: throw new NotImplementedException();
                }
            });

            var context = DiscordNetContextMapper.MapToRunContext(Context);
            var result = await _commandRunner.RunAsync(command, context);

            return new TaylorBotResult(result, context);
        }

        [Command("tracks")]
        [Summary("Gets the top tracks listened to by a user over a period.")]
        public async Task<RuntimeResult> TracksAsync(
            [Summary("What period of time would you like the top tracks for?")]
            LastFmPeriod period = LastFmPeriod.SevenDay,
            [Summary("What user would you like to see a the top tracks for?")]
            [Remainder]
            IUserArgument<IUser>? user = null
        )
        {
            var command = new Command(DiscordNetContextMapper.MapToCommandMetadata(Context), async () =>
            {
                var u = user == null ? Context.User : await user.GetTrackedUserAsync();

                var lastFmUsername = await _lastFmUsernameRepository.GetLastFmUsernameAsync(u);

                if (lastFmUsername == null)
                    return new EmbedResult(CreateLastFmNotSetEmbed(u));

                var result = await _lastFmClient.GetTopTracksAsync(lastFmUsername.Username, period);

                switch (result)
                {
                    case LastFmGenericErrorResult errorResult:
                        return new EmbedResult(CreateLastFmErrorEmbed(errorResult));

                    case TopTracksResult success:
                        if (success.TopTracks.Count > 0)
                        {
                            var formattedTracks = success.TopTracks.Select((t, index) =>
                                $"{index + 1}. {t.ArtistName.DiscordMdLink(t.ArtistUrl.ToString())} - {t.Name.DiscordMdLink(t.TrackUrl.ToString())}: {"play".ToQuantity(t.PlayCount, TaylorBotFormats.BoldReadable)}"
                            );

                            return new EmbedResult(
                                CreateBaseLastFmEmbed(lastFmUsername, u)
                                    .WithColor(TaylorBotColors.SuccessColor)
                                    .WithTitle($"Top tracks | {_lastFmPeriodStringMapper.MapLastFmPeriodToReadableString(period)}")
                                    .WithDescription(formattedTracks.CreateEmbedDescriptionWithMaxAmountOfLines())
                                    .Build()
                            );
                        }
                        else
                        {
                            return new EmbedResult(
                                CreateLastFmNoScrobbleErrorEmbed(lastFmUsername, u, period)
                            );
                        }

                    default: throw new NotImplementedException();
                }
            });

            var context = DiscordNetContextMapper.MapToRunContext(Context);
            var result = await _commandRunner.RunAsync(command, context);

            return new TaylorBotResult(result, context);
        }

        [Command("albums")]
        [Summary("Gets the top albums listened to by a user over a period.")]
        public async Task<RuntimeResult> AlbumsAsync(
            [Summary("What period of time would you like the top albums for?")]
            LastFmPeriod period = LastFmPeriod.SevenDay,
            [Summary("What user would you like to see a the top albums for?")]
            [Remainder]
            IUserArgument<IUser>? user = null
        )
        {
            var command = new Command(DiscordNetContextMapper.MapToCommandMetadata(Context), async () =>
            {
                var u = user == null ? Context.User : await user.GetTrackedUserAsync();

                var lastFmUsername = await _lastFmUsernameRepository.GetLastFmUsernameAsync(u);

                if (lastFmUsername == null)
                    return new EmbedResult(CreateLastFmNotSetEmbed(u));

                var result = await _lastFmClient.GetTopAlbumsAsync(lastFmUsername.Username, period);

                switch (result)
                {
                    case LastFmGenericErrorResult errorResult:
                        return new EmbedResult(CreateLastFmErrorEmbed(errorResult));

                    case TopAlbumsResult success:
                        if (success.TopAlbums.Count > 0)
                        {
                            var formattedAlbums = success.TopAlbums.Select((a, index) =>
                                $"{index + 1}. {a.ArtistName.DiscordMdLink(a.ArtistUrl.ToString())} - {a.Name.DiscordMdLink(a.AlbumUrl.ToString())}: {"play".ToQuantity(a.PlayCount, TaylorBotFormats.BoldReadable)}"
                            );

                            var embed = CreateBaseLastFmEmbed(lastFmUsername, u)
                                .WithColor(TaylorBotColors.SuccessColor)
                                .WithTitle($"Top albums | {_lastFmPeriodStringMapper.MapLastFmPeriodToReadableString(period)}")
                                .WithDescription(formattedAlbums.CreateEmbedDescriptionWithMaxAmountOfLines());

                            var firstImageUrl = success.TopAlbums.Select(a => a.AlbumImageUrl).FirstOrDefault(url => url != null);
                            if (firstImageUrl != null)
                                embed.WithThumbnailUrl(firstImageUrl.ToString());

                            return new EmbedResult(embed.Build());
                        }
                        else
                        {
                            return new EmbedResult(
                                CreateLastFmNoScrobbleErrorEmbed(lastFmUsername, u, period)
                            );
                        }

                    default: throw new NotImplementedException();
                }
            });

            var context = DiscordNetContextMapper.MapToRunContext(Context);
            var result = await _commandRunner.RunAsync(command, context);

            return new TaylorBotResult(result, context);
        }

        [Command("collage")]
        [Alias("c")]
        [Summary("Generates a collage based on a user's Last.Fm listening habits. Collages are provided by a third-party and might have loading problems.")]
        public async Task<RuntimeResult> CollageAsync(
            [Summary("What period of time would you like the collage for?")]
            LastFmPeriod period = LastFmPeriod.SevenDay,
            [Summary("What size (number of rows and columns) would you like the collage to be?")]
            LastFmCollageSize? size = null,
            [Summary("What user would you like to see a collage for?")]
            [Remainder]
            IUserArgument<IUser>? user = null
        )
        {
            var command = new Command(DiscordNetContextMapper.MapToCommandMetadata(Context), async () =>
            {
                var u = user == null ? Context.User : await user.GetTrackedUserAsync();

                if (size == null)
                    size = new LastFmCollageSize(3);

                var lastFmUsername = await _lastFmUsernameRepository.GetLastFmUsernameAsync(u);

                if (lastFmUsername == null)
                    return new EmbedResult(CreateLastFmNotSetEmbed(u));

                var queryString = new[] {
                    $"user={lastFmUsername.Username}",
                    $"period={_lastFmPeriodStringMapper.MapLastFmPeriodToUrlString(period)}",
                    $"rows={size.Parsed}",
                    $"cols={size.Parsed}",
                    "imageSize=400",
                    $"a={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
                };

                return new EmbedResult(new EmbedBuilder()
                    .WithColor(TaylorBotColors.SuccessColor)
                    .WithAuthor(
                        name: lastFmUsername.Username,
                        iconUrl: u.GetAvatarUrlOrDefault(),
                        url: lastFmUsername.LinkToProfile
                    )
                    .WithTitle($"Collage {size.Parsed}x{size.Parsed} | {_lastFmPeriodStringMapper.MapLastFmPeriodToReadableString(period)}")
                    .WithImageUrl($"https://lastfmtopalbums.dinduks.com/patchwork.php?{string.Join('&', queryString)}")
                .Build());
            });

            var context = DiscordNetContextMapper.MapToRunContext(Context);
            var result = await _commandRunner.RunAsync(command, context);

            return new TaylorBotResult(result, context);
        }

        private Embed CreateLastFmNotSetEmbed(IUser user)
        {
            return new EmbedBuilder()
                .WithUserAsAuthor(Context.User)
                .WithColor(TaylorBotColors.ErrorColor)
                .WithDescription(string.Join('\n', new[] {
                    $"{user.Mention}'s Last.fm username is not set. 🚫",
                    $"Last.fm can track your listening habits on any platform. You can create a Last.fm account by {"clicking here".DiscordMdLink("https://www.last.fm/join")}.",
                    $"You can then link it to TaylorBot with `{Context.CommandPrefix}lastfm set <username>`."
                }))
            .Build();
        }

        private Embed CreateLastFmErrorEmbed(LastFmGenericErrorResult error)
        {
            return new EmbedBuilder()
                .WithUserAsAuthor(Context.User)
                .WithColor(TaylorBotColors.ErrorColor)
                .WithDescription(string.Join('\n', new[] {
                    $"Last.fm returned an error. {(error.Error != null ? $"({error.Error}) " : string.Empty)}😢",
                    "The site might be down. Try again later!"
                }))
            .Build();
        }

        private static EmbedBuilder CreateBaseLastFmEmbed(LastFmUsername lastFmUsername, IUser user)
        {
            return new EmbedBuilder().WithAuthor(
                name: lastFmUsername.Username,
                iconUrl: user.GetAvatarUrlOrDefault(),
                url: lastFmUsername.LinkToProfile
            );
        }

        private Embed CreateLastFmNoScrobbleErrorEmbed(LastFmUsername lastFmUsername, IUser user, LastFmPeriod period)
        {
            return CreateBaseLastFmEmbed(lastFmUsername, user)
                .WithColor(TaylorBotColors.ErrorColor)
                .WithDescription(string.Join('\n', new[] {
                    $"This account does not have any scrobbles for period '{_lastFmPeriodStringMapper.MapLastFmPeriodToReadableString(period)}'. 🔍",
                    "Start listening to a song and scrobble it to Last.fm so it shows up here!"
                }))
                .Build();
        }
    }
}