using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SpotifyAPI.Web;

namespace Cabbage_Music
{
    public class Commands : BaseCommandModule
    {
        public SpotifyClient Spotify { get; set; }
        public Random Rand { get; set; }

        [Command("Play"), Aliases("p"), Description("Plays a song, Youtube playlist or Spotify playlist on Youtube")]
        public Task Play(CommandContext ctx, bool shuffle, [RemainingText] string song)
            => PlayCommand.Play(ctx, song, Spotify, shuffle, Rand);

        [Command("Play")]
        public Task Play(CommandContext ctx, [RemainingText] string song)
            => PlayCommand.Play(ctx, song, Spotify);

        [Command("Join"), Description("Joins the voice chat"), Aliases("j")]
        public Task Join(CommandContext ctx, DiscordChannel channel = null)
            => Music.Join(ctx, channel);

        [Command("Leave"), Aliases("stop", "l", "dc"), Description("Leaves the voice chat")]
        public Task Leave(CommandContext ctx)
            => Music.Leave(ctx);

        [Command("Queue"), Aliases("q"), Description("Take a look at your song queue")]
        public Task Queue(CommandContext ctx)
            => Music.Queue(ctx);

        [Command("remove"), Description("Removes a track from the queue")]
        public Task Remove(CommandContext ctx, int index)
            => Music.Remove(ctx, index);

        [Command("Pause"), Description("Pauses playback")]
        public Task Pause(CommandContext ctx)
            => Music.Pause(ctx);

        [Command("Resume"), Description("Resumes playback")]
        public Task Resume(CommandContext ctx)
            => Music.Resume(ctx);

        [Command("Volume"), Aliases("vol", "v"), Description("Adjusts the volume, takes input from 0 to 100"), Cooldown(1, 5, CooldownBucketType.Guild)]
        public Task Volume(CommandContext ctx, int vol)
            => Music.Volume(ctx, vol);

        [Command("Song"), Aliases("songinfo", "s", "si", "nowplaying", "np"), Description("Gives information about the currently playing song")]
        public Task Song(CommandContext ctx)
            => Music.Song(ctx);

        [Command("bassboost"), Aliases("bb"), Description("B A S S"), Cooldown(1, 5, CooldownBucketType.Guild)]
        public Task Bass(CommandContext ctx)
            => Music.BB(ctx);

        [Command("Skip"), Description("Skips to the next song in the queue or stops playback")]
        public Task Skip(CommandContext ctx)
            => Music.Skip(ctx);

        [Command("reset"), Description("Resets all equalization"), Cooldown(1, 5, CooldownBucketType.Guild)]
        public Task Reset(CommandContext ctx)
            => Music.Reset(ctx);

        [Command("shuffle"), Description("Shuffles all the songs in the queue")]
        public Task Shuffle(CommandContext ctx)
            => Music.Shuffle(ctx, Rand);

        [Command("seek"), Description("Skips ahead the specified amount of seconds (defaults to 15)")]
        public Task Seek(CommandContext ctx, float seconds = 15)
            => Music.Seek(ctx, seconds);

        [Command("loop"), Aliases("repeat"), Description("Loops the currently playing song")]
        public Task Loop(CommandContext ctx)
            => Music.Loop(ctx);

        [Command("help"), Description("Displays the help command")]
        public Task Help(CommandContext ctx, string command = null)
            => Other.Help(ctx, command);

        [Command("invite"), Description("Generates an invite for the bot")]
        public Task Invite(CommandContext ctx)
            => Other.Invite(ctx);
    }
}
