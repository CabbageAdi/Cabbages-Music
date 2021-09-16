using DSharpPlus.CommandsNext;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System;
using System.Linq;
using DSharpPlus.Lavalink;
using SpotifyAPI.Web;
using System.Collections.Generic;

namespace Cabbage_Music
{
    public class PlayCommand : BaseCommandModule
    {
        public static async Task Play(SharedContext ctx, string song, SpotifyClient Spotify, bool shuffle = false, Random rand = null)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Guild);

            if (conn == null)
            {
                var channel = ctx.Member.VoiceState.Channel;
                await node.ConnectAsync(channel);
                await ctx.Channel.SendMessageAsync($"Joined **{channel.Name}**!");
                conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
                await conn.SetVolumeAsync(50);

                Program.Queues.Remove(ctx.Guild.Id);
            }

            LavalinkLoadResult loadResult = null;

            bool isSpotify = false;
            List<string> spotifynames = new();
            List<LavalinkTrack> spotifyTracks = new();
            List<LavalinkLoadResult> spotifyresults = new();
            List<string> spotifyfailedsearches = new();

            if (Uri.IsWellFormedUriString(song, UriKind.Absolute))
            {
                string host = new Uri(song).ToString();
                if (host.ToLower().StartsWith("https://www.youtube.com/") || host.ToLower().StartsWith("https://youtu.be/"))
                {
                    loadResult = await node.Rest.GetTracksAsync(new Uri(song));
                }
                else if (host.ToLower().StartsWith("https://open.spotify.com/playlist"))
                {
                    await ctx.RespondAsync("Retrieving playlist data... (Please wait a few seconds)");

                    string id = host.Substring("https://open.spotify.com/playlist/".Length);
                    int occurence = id.IndexOf('?');
                    id = id.Substring(0, occurence);

                    var strack = await Spotify.Playlists.GetItems(id);
                    var items = strack.Items;
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items.ElementAt(i).Track is FullTrack fgsrgs)
                        {
                            if (items.ElementAt(i).IsLocal)
                            {
                                spotifynames.Add($"{fgsrgs.Name}");
                            }
                            else
                            {
                                string artists = "";
                                foreach (var artist in fgsrgs.Artists)
                                {
                                    artists += artist.Name + " ";
                                }
                                spotifynames.Add($"{fgsrgs.Name} {artists}");
                            }
                        }
                    }

                    List<Task<LavalinkLoadResult>> Tasks = new(); 

                    for (int i = 0; i < spotifynames.Count; i++)
                    {
                        Tasks.Add(node.Rest.GetTracksAsync(spotifynames[i]));
                    }

                    await Task.WhenAll(Tasks);

                    for (int i = 0; i < spotifynames.Count; i++)
                    {
                        spotifyresults.Add(Tasks[i].Result);
                        if (spotifyresults[i].LoadResultType == LavalinkLoadResultType.LoadFailed)
                        {
                            spotifyfailedsearches.Add(spotifynames[i] + "(Load Failed)");
                        }
                        else if (spotifyresults[i].LoadResultType == LavalinkLoadResultType.NoMatches)
                        {
                            spotifyfailedsearches.Add(spotifynames[i] + "(No Matches Found)");
                        }
                        else
                        {
                            spotifyTracks.Add(spotifyresults[i].Tracks.First());
                        }
                    }

                    isSpotify = true;
                    if (spotifyfailedsearches.Count > 0)
                    {
                        var embeds = new DiscordEmbedBuilder
                        {
                            Title = "Track searches failed for these tracks.",
                            Description = string.Join('\n', spotifyfailedsearches)
                        };
                        await ctx.RespondAsync(embed: embeds);
                    }
                }
                else if (host.ToLower().StartsWith("https://open.spotify.com/track"))
                {
                    string id = host.Substring("https://open.spotify.com/track/".Length);
                    int occurence = id.IndexOf('?');
                    id = id.Substring(0, occurence);

                    var spotifyTrack = await Spotify.Tracks.Get(id);

                    loadResult = await node.Rest.GetTracksAsync(spotifyTrack.IsLocal ? spotifyTrack.Name : $"{spotifyTrack.Name} {string.Join(" ", spotifyTrack.Artists.Select(a => a.Name))}");

                    if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed)
                    {
                        await ctx.RespondAsync($"Track failed to load, please try again later or check for misspelling and wrong links");
                        return;
                    }
                    if (loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                    {
                        await ctx.RespondAsync($"Track search failed for {song}.");
                        return;
                    }
                }
                else
                {
                    loadResult = await node.Rest.GetTracksAsync(song, LavalinkSearchType.Youtube);
                    if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed)
                    {
                        await ctx.RespondAsync($"Track failed to load, please try again later or check for misspelling invalid links and private playlists");
                        return;
                    }
                    if (loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                    {
                        await ctx.RespondAsync($"Track search failed for {song}.");
                        return;
                    }
                }
            }
            else
            {
                loadResult = await node.Rest.GetTracksAsync(song, LavalinkSearchType.Youtube);
                if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed)
                {
                    await ctx.RespondAsync($"Track failed to load, please try again later or check for misspelling and wrong links");
                    return;
                }
                if (loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.RespondAsync($"Track search failed for {song}.");
                    return;
                }
            }


            void write(LavalinkTrack track)
            {
                if (!Program.Queues.ContainsKey(ctx.Guild.Id))
                    Program.Queues[ctx.Guild.Id] = new();
                Program.Queues[ctx.Guild.Id].Add(track);
            }

            LavalinkTrack track = null;

            if (isSpotify == true)
            {
                if (shuffle)
                    spotifyTracks = spotifyTracks.OrderBy(x => rand.Next()).ToList();
                for (int i = 0; i < spotifyTracks.Count; i++)
                {
                    if (i == 0)
                        track = spotifyTracks[i];
                    write(spotifyTracks[i]);
                }
            }
            else if (loadResult.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
            {
                var tracks = loadResult.Tracks.ToList();
                if (shuffle)
                    tracks = tracks.OrderBy(x => rand.Next()).ToList();
                for (int i = 0; i < loadResult.Tracks.Count(); i++)
                {
                    if (i == 0)
                        track = tracks[i];
                    write(tracks[i]);
                }
                await ctx.RespondAsync("Added playlist");
            }
            else
            {
                track = loadResult.Tracks.First();
                write(track);
            }

            if (conn.CurrentState.CurrentTrack != null)
            {
                var embedd = new DiscordEmbedBuilder
                {
                    Title = "Added to queue",
                    ImageUrl = $"https://img.youtube.com/vi/{track.Identifier}/mqdefault.jpg",
                    Description = $"[{track.Title}]({track.Uri} \"Link for the song\")",
                    Color = DiscordColor.Red
                };
                embedd.WithFooter("On YouTube");
                await ctx.RespondAsync(embed: embedd);
                return;
            }

            await conn.PlayAsync(track);

            var embed = new DiscordEmbedBuilder
            {
                Title = "Now playing",
                ImageUrl = $"https://img.youtube.com/vi/{track.Identifier}/mqdefault.jpg",
                Description = $"[{track.Title}]({track.Uri} \"Link for the song\")",
                Color = DiscordColor.Red
            };
            embed.WithFooter("On YouTube");
            await ctx.RespondAsync(embed: embed);
            conn.PlaybackFinished += async (s, e) =>
            {
                e.Handled = true;
                var finishembed = new DiscordEmbedBuilder
                {
                    Title = "Finished Playing",
                    Description = $"[{e.Track.Title}]({e.Track.Uri} \"Link for the song\")",
                    Color = DiscordColor.Black
                };
                await ctx.Channel.SendMessageAsync(finishembed);

                if (!Program.Loop.TryGetValue(ctx.Guild.Id, out var loop) || !loop)
                {
                    if (!Program.Queues.ContainsKey(ctx.Guild.Id)) return;

                    if (Program.Queues[ctx.Guild.Id].Count == 1) return;

                    Program.Queues[ctx.Guild.Id].RemoveAt(0);
                }

                var finaltrack = Program.Queues[ctx.Guild.Id][0];

                var playingembed = new DiscordEmbedBuilder
                {
                    Title = "Now Playing",
                    Description = $"[{finaltrack.Title}]({finaltrack.Uri} \"Link for the song\")",
                    Color = DiscordColor.Red
                };
                await ctx.Channel.SendMessageAsync(playingembed);
                await conn.PlayAsync(finaltrack);
            };
        }
    }
}
