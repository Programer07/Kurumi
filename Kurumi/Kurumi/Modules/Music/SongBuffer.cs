using Discord;
using Kurumi.Common;
using Kurumi.Modules.Music.Services;
using Kurumi.Services.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Kurumi.Modules.Music
{
    public class SongBuffer : IDisposable
    {
        private readonly MusicProvider Provider;
        private readonly Process DownloadProcess;
        private readonly Process StreamProcess;
        private readonly string Location;
        private readonly string Url;
        public Stream outStream; 
        
        public SongBuffer(ISongInfo song, IGuild guild)
        {
            try
            {
                Provider = song.Provider;
                Url = song.StreamUrl;

                if (Provider != MusicProvider.ListenMoe)
                {
                    string[] Files = Directory.GetFiles(KurumiPathConfig.MusicTemp);
                    for (int i = 0; i < Files.Length; i++)
                        if (Files[i].Contains(guild.Id.ToString()))
                            File.Delete(Files[i]);

                    Location = $"{KurumiPathConfig.MusicTemp}{guild.Id}";
                    string Id = YoutubeService.VideoId(song.StreamUrl);
                    if (!YoutubeCacheManager.TryGetCache(Id, guild.Id))
                    { //Song is not cached
                        DownloadProcess = DownloadSong();
                        while (!DownloadProcess.HasExited)
                            Thread.Sleep(50);

                        YoutubeCacheManager.Cache(Location, Id); //Cache the file
                    }
                }
                else
                    Location = song.StreamUrl;

                StreamProcess = StartStream();
                outStream = StreamProcess.StandardOutput.BaseStream;
            }
            catch(Win32Exception)
            {
                Utilities.Log(new LogMessage(LogSeverity.Error, "SongBuffer", "FFPMEG or Youtube-Dl not found!"));
            }
            catch(OperationCanceledException) { }
            catch(InvalidOperationException) { }
            catch (Exception ex)
            {
                Utilities.Log(new LogMessage(LogSeverity.Error, "SongBuffer", null, ex));
            }
        }

        public void Dispose()
        {
            try
            {
                StreamProcess.StandardOutput.Dispose();
            }
            catch { }
            outStream.Dispose();
            try
            {
                StreamProcess.Kill();
            }
            catch { }
            StreamProcess.Dispose();

            if (Provider != MusicProvider.ListenMoe)
            {
                try
                {
                    DownloadProcess.Kill();
                }
                catch { }
                try
                {
                    DownloadProcess.Dispose();
                }
                catch { }
                try
                {
                    Thread.Sleep(100); //Fails to delete it immediately
                    if (File.Exists(Location))
                        File.Delete(Location);
                }
                catch { }
            }
        }

        private Process DownloadSong()
        {
            string args = $"--output {'"'}{Location}{'"'} -i -f bestaudio --audio-quality 0 {'"'}{Url}{'"'}";
            return Process.Start(new ProcessStartInfo
            {
                FileName = $"{KurumiPathConfig.Bin}youtube-dl",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
            });
        }
        private Process StartStream()
        {
            string args = $"-i {Location} -f s16le -ar 48000 -vn -ac 2 pipe:1";
            return Process.Start(new ProcessStartInfo
            {
                FileName = $"{KurumiPathConfig.Bin}ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });
        }
    }
}