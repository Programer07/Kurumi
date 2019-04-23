using Discord;
using Discord.Audio;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Random;
using Kurumi.StartUp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kurumi.Modules.Music
{
    public class MusicPlayer
    {
        public int SkipVotes = 0;
        public float Volume;
        public bool Random = false;
        public bool Repeat = false;
        public readonly List<ISongInfo> Queue;
        public ISongInfo Song { get; private set; }

        private IAudioClient Ac;
        private SongBuffer sBuffer;
        private bool _Pause = false;
        private bool _Leave = false;
        private CancellationTokenSource CancelStream;
        private CancellationTokenSource CancelPause;

        private readonly ICommandContext Context;
        private readonly Thread playerThread;
        private readonly IVoiceChannel TargetChannel;
        //private readonly LanguageDictionary lang;
        private readonly KurumiRandom rng;

        private string FormatedVolume
            => $"{Volume * 100}%";

        public MusicPlayer(ICommandContext Context, IVoiceChannel TargetChannel, float Volume, ISongInfo Song)
        {
            this.Context = Context;
            this.Volume = Volume;
            this.TargetChannel = TargetChannel;
            Queue = new List<ISongInfo>();
            CancelStream = new CancellationTokenSource();
            CancelPause = new CancellationTokenSource();
            rng = new KurumiRandom();

            //Add the first song
            Queue.Add(Song);

            //Start playing
            playerThread = new Thread(new ThreadStart(() => SendAudio()))
            {
                Priority = ThreadPriority.Highest
            };
            playerThread.Start();
        }


        public async void SendAudio()
        {
            LanguageDictionary lang = Language.GetLanguage(Context.Guild);
            try
            {
                //Connect voice
                await ConnectVoice();
                if (Ac == null) //Failed to connect
                    return;

                //Create PCM stream
                using (AudioOutStream pcm = Ac.CreatePCMStream(AudioApplication.Music, 100000, 1, 0)) //100kbps
                {
                    uint PlayCount = 0;
                Play:
                    PlayCount++;
                    //Get the language (it has to be updated every time)
                    lang = Language.GetLanguage(Context.Guild);

                    //Get song
                    if (!Repeat)
                        Song = GetNextSong();

                    //Send info
                    var MusicInfoEmbed = new EmbedBuilder()
                        .WithColor(Config.EmbedColor)
                        .WithDescription(lang["music_title", "TITLE", Song.Title])
                        .WithThumbnailUrl(Song.ThumbnailUrl)
                        .AddField(lang["music_duration"], Song.FormatedDuration, true)
                        .AddField(lang["music_volume"], FormatedVolume, true)
                        .AddField(lang["music_requested"], Song.User.Username, true)
                        .AddField(lang["music_position"], lang["music_now_playing"], true);
                    await Context.Channel.SendEmbedAsync(MusicInfoEmbed);

                    //Buffer the song
                    sBuffer = new SongBuffer(Song, Context.Guild);

                    //Log
                    await Utilities.Log(new LogMessage(LogSeverity.Info, "MusicPlayer", "Starting stream."), Context);

                    //Send bytes
                    try
                    {
                        byte[] buffer = new byte[81920];
                        int count;
                        while ((count = await sBuffer.outStream.ReadAsync(buffer, 0, 81920, CancelStream.Token).ConfigureAwait(false)) > 0)
                        {
                            if (_Pause)
                            {
                                try
                                {
                                    await Task.Delay(Timeout.Infinite, CancelPause.Token);
                                }
                                catch { }
                                CancelPause = new CancellationTokenSource();
                                _Pause = false;
                            }
                            await pcm.WriteAsync(ScaleVolumeUnsafeAllocateBuffers(buffer, Volume), 0, count, CancelStream.Token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) { }
                    finally
                    {
                        sBuffer.Dispose();
                        SkipVotes = 0; //Reset skips for new song
                    }

                    if (_Leave)
                        goto Dispose; //Left voice channel, dispose what needs to be disposed then exit

                    if (CancelStream.IsCancellationRequested) //If the stream was cancelled and reached this point it was a skip
                        Repeat = false;

                    if (Repeat) //Play the same song again
                    {
                        if (Song.Provider == MusicProvider.ListenMoe) //Listen moe is down
                            goto Dispose;
                        else
                            goto Play;
                    }
                    else
                    {
                        if (Queue.Count > 0) //Check if there are songs in the queue
                        {
                            CancelStream = new CancellationTokenSource();
                            if (Random && Queue.Count > 1)
                            {
                                await Context.Channel.SendEmbedAsync(lang["music_playing_next_random"]);
                                await Utilities.Log(new LogMessage(LogSeverity.Info, "MusicPlayer", "Playing next."), Context);
                            }
                            else
                            {
                                if (Random) //Disable random
                                    Random = false;

                                await Context.Channel.SendEmbedAsync(lang["music_playing_next"]);
                                await Utilities.Log(new LogMessage(LogSeverity.Info, "MusicPlayer", "Playing next."), Context);
                            }
                            goto Play;
                        }
                        else
                        {
                            //No songs left
                            if (PlayCount == 1)
                                await Context.Channel.SendEmbedAsync(lang["music_finished"]);
                            else
                                await Context.Channel.SendEmbedAsync(lang["music_finished_playlist"]);
                            await Utilities.Log(new LogMessage(LogSeverity.Info, "MusicPlayer", "Song finished."), Context);
                        }
                    }

                //Dispose everything
                Dispose:
                    await Ac.StopAsync();
                    if (!CancelStream.IsCancellationRequested) //If the stream is cancelled the flush will never exit.
                        await pcm.FlushAsync();
                    pcm.Dispose();
                    Ac.Dispose();
                    CancelStream.Dispose();
                    CancelPause.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("50001: Missing Access"))
                {
                    await Utilities.Log(new LogMessage(LogSeverity.Warning, "MusicPlayer", "Missing access."), Context);
                    await Context.Channel.TrySendEmbedAsync(lang["music_missing_access"]);
                    return;
                }
                else if (ex.Message.Contains("10003: Unknown Channel"))
                {
                    await Utilities.Log(new LogMessage(LogSeverity.Warning, "MusicPlayer", "Voice channel not found."), Context);
                    await Context.Channel.TrySendEmbedAsync(lang["music_channel_not_found"]);
                    return;
                }

                await Utilities.Log(new LogMessage(LogSeverity.Error, "MusicPlayer", null, ex), Context);
            }
            Music.MusicPlayers.TryRemove(Context.Guild.Id, out _); //Remove music player
        }
        private async Task ConnectVoice(IVoiceChannel channel = null)
        {
            var lang = Language.GetLanguage(Context.Guild);
            try
            {
                IVoiceChannel target = channel;
                if (target == null)
                    target = TargetChannel;

                int retries = 2; //Sometimes discord.net throws TimeoutException, try reconnecting 2 more times.
                Reconnect:
                try
                {
                    Ac = await target.ConnectAsync().ConfigureAwait(false);
                }catch(TimeoutException)
                {
                    if (retries == 0)
                    {
                        await Context.Channel.SendEmbedAsync(lang["music_voice_fail"]);
                        return;
                    }
                    retries--;
                    goto Reconnect;
                }
            }
            catch(Exception ex)
            {
                if (ex.Message.Contains("50001: Missing Access")) //Bot doesn't have permission to join voice channel
                {
                    await Context.Channel.SendEmbedAsync(lang["music_no_join_permission"]);
                    return;
                }
                await Utilities.Log(new LogMessage(LogSeverity.Error, "MusicPlayer", null, ex), Context);
            }
        }
        public bool Dequeue(int Index)
        {
            if (Queue.Count > Index)
            {
                Queue.RemoveAt(Index);
                return true;
            }
            return false;
        }
        public ISongInfo GetSong(string Url)
        {
            for (int i = 0; i < Queue.Count; i++)
            {
                if (Queue[i].StreamUrl == Url)
                    return Queue[i];
            }
            return null;
        }
        public bool Pause()
        {
            if (_Pause)
                CancelPause.Cancel();
            else
                _Pause = true;
            return _Pause;
        }
        public bool Next()
        {
            if (Queue.Count == 0)
                return false;
            CancelStream.Cancel(); //Canceling without seting leave to true
            return true;
        }
        public void Leave()
        {
            _Leave = true;
            CancelStream.Cancel();
        }
        public bool VoiceMatch(IVoiceChannel channel) => (SelfVoiceChannel?.Id ?? 1) == (channel?.Id ?? 0);
        private IVoiceChannel SelfVoiceChannel => Context.Guild.GetUserAsync(Program.Bot.DiscordClient.CurrentUser.Id).Result?.VoiceChannel;
        private unsafe byte[] ScaleVolumeUnsafeAllocateBuffers(byte[] audioSamples, float volume)
        {
            var output = new byte[audioSamples.Length];
            if (Math.Abs(volume - 1f) < 0.0001f)
            {
                Buffer.BlockCopy(audioSamples, 0, output, 0, audioSamples.Length);
                return output;
            }

            // 16-bit precision for the multiplication
            int volumeFixed = (int)Math.Round(volume * 65536d);

            int count = audioSamples.Length / 2;

            fixed (byte* srcBytes = audioSamples)
            fixed (byte* dstBytes = output)
            {
                short* src = (short*)srcBytes;
                short* dst = (short*)dstBytes;

                for (int i = count; i != 0; i--, src++, dst++)
                    *dst = (short)(((*src) * volumeFixed) >> 16);
            }

            return output;
        }
        public ISongInfo GetNextSong()
        {
            if (Queue.Count == 0)
                return null;

            int i = 0;
            if (Random)
                i = rng.Next(0, Queue.Count);

            var s = Queue[i];
            Queue.RemoveAt(i);
            return s;
        }
    }

    public enum MusicProvider
    {
        Youtube,
        SoundCloud,
        ListenMoe,
    }
}