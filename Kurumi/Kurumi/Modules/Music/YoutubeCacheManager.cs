using Discord;
using Kurumi.Common;
using Kurumi.Services.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Kurumi.Modules.Music
{
    public static class YoutubeCacheManager
    {
        private static readonly object CacheLock = new object();
        public static bool TryGetCache(string Id, ulong Guild)
        {
            try
            {
                string Path = $"{KurumiPathConfig.YoutubeCache}{Id}";
                if (File.Exists(Path)) //Video is cached
                {
                    lock (CacheLock)
                    {
                        string TargetPath = $"{KurumiPathConfig.MusicTemp}{Guild}";
                        File.Copy(Path, TargetPath); //Move to the music temp
                        return true;
                    }
                }
            }
            catch (Exception)
            { }
            return false;
        }

        public static void Cache(string Path, string Id)
        {
            try
            {
                if (Config.YoutubeCacheSize == 0) //0 - Disabled
                    return;

                lock (CacheLock)
                {
                    string TargetPath = $"{KurumiPathConfig.YoutubeCache}{Id}";
                    if (!File.Exists(TargetPath)) //Video isn't cached
                        File.Copy(Path, TargetPath);
                }
                ManageSize();
            }
            catch (Exception ex)
            {
                Utilities.Log(new LogMessage(LogSeverity.Warning, "Youtube Cache", $"Caching error:\n", ex), null);
            }
        }

        private static void ManageSize()
        {
            try
            {
                long Size = GetDirectorySize(KurumiPathConfig.YoutubeCache);
                long Limit = (long)Config.YoutubeCacheSize * 1000000000;

                if (Size <= Limit)
                    return;

                lock (CacheLock)
                {
                    var OrderedFiles = GetFilesOrdered(KurumiPathConfig.YoutubeCache);
                    while (Size > Limit)
                    {
                        var Current = OrderedFiles[0];
                        File.Delete(Current.FullName);
                        Size -= Current.Length;
                    }
                }
            }
            catch(Exception ex)
            {
                Utilities.Log(new LogMessage(LogSeverity.Error, "Youtube Cache", null, ex), null);
            }
        }

        private static long GetDirectorySize(string p)
        {
            string[] files = Directory.GetFiles(p);

            long size = 0;
            foreach (string file in files)
            {
                FileInfo info = new FileInfo(file);
                size += info.Length;
            }

            return size;
        }

        private static List<FileInfo> GetFilesOrdered(string path)
        {
            string[] files = Directory.GetFiles(path);

            List<FileInfo> f = new List<FileInfo>();
            foreach (string file in files)
            {
                f.Add(new FileInfo(file));
            }

            return f.OrderBy(x => x.CreationTime).ToList();
        }
    }
}