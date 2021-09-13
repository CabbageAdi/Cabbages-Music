using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System;
using System.Linq;
using DSharpPlus.Lavalink;
using SpotifyAPI.Web;
using System.Text;
using System.Collections.Generic;

namespace Cabbage_Music
{
    public class Music : BaseCommandModule
    {
        public Random Rand { get; set; }

        [Command("Join"), Description("Joins the voice chat"), Aliases("j")]
        public async Task Join(CommandContext ctx, DiscordChannel channel = null)
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

            await node.ConnectAsync(channel);
            await ctx.RespondAsync($"Joined **{channel.Name}**!");
            await node.GetGuildConnection(ctx.Guild).SetVolumeAsync(50);
            //await ctx.Guild.CurrentMember.ModifyAsync(x => x.Deafened = true);

            if (Program.Queues.ContainsKey(ctx.Guild.Id))
                Program.Queues.Remove(ctx.Guild.Id);
        }

        [Command("Leave"), Aliases("stop", "l"), Description("Leaves the voice chat")]
        public async Task Leave(CommandContext ctx)
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

        [Command("Queue"), Aliases("q"), Description("Take a look at your song queue")]
        public async Task Queue(CommandContext ctx)
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

            if (queue.Count > 0)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Song queue",
                    Color = DiscordColor.Blue
                };
                int count = 0;
                for (int i = 0; i < queue.Count; i++)
                {
                    if (i > 10)
                    {
                        count += 1;
                    }
                    else
                    {
                        var track = queue[i];
                        embed.Description = embed.Description + $"{i + 1}. [{track.Title}](https://www.youtube.com/watch?v={track.Identifier})\n";
                    }
                }
                if (count > 0)
                {
                    if (count == 1)
                    {
                        embed.Description = embed.Description + $"+{count} song";
                    }
                    else
                    {
                        embed.Description = embed.Description + $"+{count} songs";
                    }
                }
                await ctx.RespondAsync(embed: embed);
            }
            else
            {
                await ctx.RespondAsync("There's nothing in the queue");
            }
        }

        [Command("remove"), Description("Removes a track from the queue")]
        public async Task Remove(CommandContext ctx, int index)
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

            Program.Queues[ctx.Guild.Id].RemoveAt(index - 1);
            await ctx.RespondAsync("Removed track!");
        }

        [Command("Pause"), Description("Pauses playback")]
        public async Task Pause(CommandContext ctx)
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
            //await ctx.RespondAsync("Paused");
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":pause_button:"));
        }

        [Command("Resume"), Description("Resumes playback")]
        public async Task Resume(CommandContext ctx)
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
            //await ctx.RespondAsync("Resumed");
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":arrow_forward:"));
        }

        [Command("Volume"), Aliases("vol", "v"), Description("Adjusts the volume, takes input from 0 to 100"), Cooldown(1, 5, CooldownBucketType.Guild)]
        public async Task Volume(CommandContext ctx, int vol)
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

            await conn.SetVolumeAsync(vol);
            await ctx.RespondAsync($"Set volume to {vol}%");
        }

        [Command("Song"), Aliases("songinfo", "s", "si", "nowplaying", "np"), Description("Gives information about the currently playing song")]
        public async Task Song(CommandContext ctx)
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

        [Command("Skip"), Description("Skips to the next song in the queue or stops playback")]
        public async Task Skip(CommandContext ctx)
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

        [Command("bassboost"), Aliases("bb"), Description("B A S S"), Cooldown(1, 5, CooldownBucketType.Guild)]
        public async Task BB(CommandContext ctx)
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

        [Command("reset"), Description("Resets all equalization"), Cooldown(1, 5, CooldownBucketType.Guild)]
        public async Task Reset(CommandContext ctx)
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

        [Command("shuffle"), Description("Shuffles all the songs in the queue")]
        public async Task Shuffle(CommandContext ctx)
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
            list.AddRange(Program.Queues[ctx.Guild.Id].Skip(1).OrderBy(x => Rand.Next()));
            Program.Queues[ctx.Guild.Id] = list;

            await ctx.RespondAsync("Shuffled queue");
        }

        [Command("seek"), Description("Skips ahead the specified amount of seconds (defaults to 15)")]
        public async Task Pitch(CommandContext ctx, float seconds = 15)
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

        [Command("loop"), Aliases("repeat"), Description("Loops the currently playing song")]
        public async Task Loop(CommandContext ctx)
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
