using System.Threading.Tasks;
using DSharpPlus.Entities;
using System;
using System.Linq;
using DSharpPlus.Lavalink;
using System.Collections.Generic;
using DSharpPlus.Interactivity;

namespace Cabbage_Music
{
    public class Music
    {
        public static async Task Join(SharedContext ctx, DiscordChannel channel = null)
        {
            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            if (ctx.Member.VoiceState == null)
            {
                await ctx.RespondAsync("You aren't connected to a voice channel!");
                return;
            }
            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You aren't connected to a voice channel!");
                return;
            }
            if (channel == null)
            {
                channel = ctx.Member?.VoiceState.Channel;
            }

            var conn = await node.ConnectAsync(channel);
            await ctx.RespondAsync($"Joined **{channel.Name}**!");
            await conn.SetVolumeAsync(50);

            if (Program.Queues.ContainsKey(ctx.Guild.Id))
                Program.Queues.Remove(ctx.Guild.Id);
        }

        public static async Task Leave(SharedContext ctx)
        {
            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            if (ctx.Member.VoiceState == null)
            {
                await ctx.RespondAsync("You aren't connected to a voice channel!");
                return;
            }

            var conn = node.GetGuildConnection(ctx.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Left **{conn.Channel.Name}**!");

            if (Program.Queues.ContainsKey(ctx.Guild.Id))
                Program.Queues.Remove(ctx.Guild.Id);
            if (Program.Loop.ContainsKey(ctx.Guild.Id))
                Program.Loop.Remove(ctx.Guild.Id);
        }

        public static async Task Queue(SharedContext ctx)
        {
            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            if (!Program.Queues.ContainsKey(ctx.Guild.Id))
            {
                await ctx.RespondAsync("There's nothing in the queue!");
                return;
            }
            if (Program.Queues[ctx.Guild.Id].Count == 0)
            {
                await ctx.RespondAsync("There's nothing in the queue!");
                return;
            }
            var queue = Program.Queues[ctx.Guild.Id];

            var pages = new List<Page>();

            if (queue.Count > 0)
            {
                List<List<LavalinkTrack>> split = new();
                for (int i = 0; i < queue.Count; i++)
                {
                    if (i % 10 == 0)
                        split.Add(new());
                    split[(i - (i % 10)) / 10].Add(queue[i]);
                }
                int counter = 0;
                foreach (var page in split)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Song queue",
                        Color = DiscordColor.Blue
                    };

                    for (int i = 0; i < page.Count; i++)
                    {
                        var track = page[i];
                        embed.Description += $"{counter + i + 1}. [{track.Title}](https://www.youtube.com/watch?v={track.Identifier})\n";
                    }

                    pages.Add(new Page(embed: embed));
                    counter += 10;
                }
            }
            else
            {
                await ctx.RespondAsync("There's nothing in the queue");
                return;
            }

            await ctx.SendPaginatedResponseAsync(pages);
        }

        public static async Task Remove(SharedContext ctx, long index)
        {
            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            if (!Program.Queues.ContainsKey(ctx.Guild.Id) || Program.Queues[ctx.Guild.Id].Count == 0 || Program.Queues[ctx.Guild.Id].Count < index)
            {
                await ctx.RespondAsync("The queue does not have a track with the given index!");
                return;
            }

            if (index <= 1)
            {
                await ctx.RespondAsync("Cannot remove track with this index.");
            }

            Program.Queues[ctx.Guild.Id].RemoveAt((int)index - 1);
            await ctx.RespondAsync("Removed track!");
        }

        public static async Task Pause(SharedContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.PauseAsync();
            await ctx.RespondAsync("Paused.");
        }

        public static async Task Resume(SharedContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.ResumeAsync();
            await ctx.RespondAsync("Resumed.");
        }

        public static async Task Volume(SharedContext ctx, long vol)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            if (vol < 0)
            {
                await ctx.RespondAsync("The volume cannot be less than 0!");
                return;
            }
            if (vol > 100)
            {
                await ctx.RespondAsync("The volume cannot be more than 100!");
                return;
            }

            await conn.SetVolumeAsync((int)vol);
            await ctx.RespondAsync($"Set volume to {vol}%");
        }

        public static async Task Song(SharedContext ctx)
        {
            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("Nothing is playing!");
                return;
            }

            var done = conn.CurrentState.PlaybackPosition;
            var whole = conn.CurrentState.CurrentTrack.Length;
            var track = conn.CurrentState.CurrentTrack;

            var embed = new DiscordEmbedBuilder
            {
                Title = "Song info",
                Description = $"Playing [{track.Title}]({track.Uri.ToString()})\n\n{ done.Hours.ToString("00") }:{ done.Minutes.ToString("00")}:{ done.Seconds.ToString("00")}/{ whole.Hours.ToString("00")}:{ whole.Minutes.ToString("00")}:{ whole.Seconds.ToString("00")}\nPlayed { (done / whole * 100).ToString("0")}%",
                ImageUrl = $"https://img.youtube.com/vi/{track.Identifier}/mqdefault.jpg"
            };
            await ctx.RespondAsync(embed: embed);
        }

        public static async Task Skip(SharedContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("Nothing is playing!");
                return;
            }

            await conn.StopAsync();
        }

        public static async Task BB(SharedContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            await conn.AdjustEqualizerAsync(new LavalinkBandAdjustment(1, 0.2f));
            await conn.AdjustEqualizerAsync(new LavalinkBandAdjustment(2, 0.2f));
            await conn.AdjustEqualizerAsync(new LavalinkBandAdjustment(3, 0.2f));
            await conn.AdjustEqualizerAsync(new LavalinkBandAdjustment(0, 0.2f));
            await ctx.RespondAsync("Enjoy da B A S S\nThe changes should come into effect in a few seconds");
        }

        public static async Task Reset(SharedContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            await conn.ResetEqualizerAsync();
            await ctx.RespondAsync("Enjoy the unaltered audio\nThe changes should come into effect in a few seconds");
        }

        public static async Task Shuffle(SharedContext ctx, Random rand)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            if (!Program.Queues.ContainsKey(ctx.Guild.Id) || Program.Queues[ctx.Guild.Id].Count == 0)
            {
                await ctx.RespondAsync("There's nothing in the queue");
                return;
            }

            var list = new List<LavalinkTrack> { Program.Queues[ctx.Guild.Id][0] };
            list.AddRange(Program.Queues[ctx.Guild.Id].Skip(1).OrderBy(x => rand.Next()));
            Program.Queues[ctx.Guild.Id] = list;

            await ctx.RespondAsync("Shuffled queue");
        }

        public static async Task Seek(SharedContext ctx, double seconds = 15)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var node = ctx.Client.GetLavalink().GetIdealNodeConnection();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("Nothing is playing!");
                return;
            }

            string s = seconds.ToString("0.00");
            string[] parts = s.Split('.');
            int second = int.Parse(parts[0]);
            int millisecond = int.Parse(parts[1]);

            TimeSpan current = conn.CurrentState.PlaybackPosition;
            await conn.SeekAsync(current + new TimeSpan(0, 0, 0, second, millisecond));
            await ctx.RespondAsync($"Skipped forward {second}.{millisecond}s!");
        }

        public static async Task Loop(SharedContext ctx)
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
                await ctx.RespondAsync("The bot is not connected to a voice channel!");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("Nothing is playing!");
                return;
            }

            if (Program.Loop.TryGetValue(ctx.Guild.Id, out var value) && value == true)
            {
                Program.Loop[ctx.Guild.Id] = false;
                await ctx.RespondAsync("Stopped looping!");
            }
            else
            {
                Program.Loop[ctx.Guild.Id] = true;
                await ctx.RespondAsync("Looped this song!");
            }
        }
    }
}
