using Kurumi.Services.Database;
using Kurumi.StartUp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Kurumi.Common
{
    public static class Config
    {
        //Not in config file
        public static bool SSLEnabled { get; set; } = true;
        private static Dictionary<string, object> RawConfig;


        //Config file
        public static KurumiEnvironment Environment { get; private set; }
        public static string BotToken { get; private set; }
        public static string IbSearchApiKey { get; private set; }
        public static string YoutubeApiKey { get; private set; }
        public static string BotlistApiKey { get; private set; }
        public static string OsuApiKey { get; private set; }
        public static string SentryApiKey { get; private set; }
        public static string RandomOrgApiKey { get; private set; }
        public static string AniListClientId { get; private set; }
        public static string AniListSecret { get; private set; }
        public static string MongoDBUsername { get; private set; }
        public static string MongoDBPassword { get; private set; }
        public static string MongoDBIp { get; private set; }
        public static string MongoDBPort { get; private set; }
        public static string MongoDBName { get; private set; }
        public static uint EmbedColor { get; private set; }
        public static string DefaultLanguage { get; set; }
        public static decimal YoutubeCacheSize { get; set; }
        public static int LoggerMode { get; set; }
        public static byte ShardCount { get; set; }
        public static ulong[] Administrators { get; private set; }

        /// <summary>
        /// Tries to load config file
        /// </summary>
        /// <param name="ex"></param>
        /// <returns>True => Success, False => fail</returns>
        public static bool TryLoad(out Exception ex)
        {
            ex = null;
            try
            {
                string ConfigContent = File.ReadAllText($"{KurumiPathConfig.Settings}Config.json");
                RawConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(ConfigContent);

                //Required settings
                object cfg = new object();
                ValidateConfig("BotToken", ref cfg, typeof(string));
                BotToken = (string)cfg;

                ValidateConfig("YoutubeKey", ref cfg, typeof(string));
                YoutubeApiKey = (string)cfg;

                ValidateConfig("ShardCount", ref cfg, typeof(long));
                ShardCount = Convert.ToByte(cfg);

                //Optional settings
                ValidateConfig("YoutubeCacheSizeGB", ref cfg, typeof(long), false, 0, "Youtube cache size is empty! Using default setting: 0 - disabled.");
                YoutubeCacheSize = Convert.ToByte(cfg);

                ValidateConfig("EmbedColor", ref cfg, typeof(long), false, 0xFF6249, "No default embed color is set! Using the default. (0xFF6249)");
                EmbedColor = Convert.ToUInt32(cfg);

                ValidateConfig("IbSearchKey", ref cfg, typeof(string), false, null, "IbSearch is disabled!");
                IbSearchApiKey = cfg?.ToString();

                ValidateConfig("BotlistKey", ref cfg, typeof(string), false, null, "Discord bot list api key is empty! (Can be ignored)");
                BotlistApiKey = cfg?.ToString();

                ValidateConfig("OsuKey", ref cfg, typeof(string), false, null, "Osu! features are disabled!");
                OsuApiKey = cfg?.ToString(); ;

                ValidateConfig("SentryKey", ref cfg, typeof(string), false, null, "Sentry error tracking is disabled!");
                SentryApiKey = cfg?.ToString();

                ValidateConfig("RandomOrgKey", ref cfg, typeof(string), false, null, "Random.org random number generator is disabled!");
                RandomOrgApiKey = cfg?.ToString();

                ValidateConfig("AniListClientId", ref cfg, typeof(string), false, null, "AniList features are disabled!");
                AniListClientId = cfg?.ToString();
                if (AniListClientId != null)
                {
                    ValidateConfig("AniListSecret", ref cfg, typeof(string), false, null, "AniList features are disabled!");
                    AniListSecret = cfg?.ToString();
                    if (AniListSecret == null)
                        AniListClientId = null;
                }

                ValidateConfig("Administrators", ref cfg, typeof(JArray), false, null, "No administrators are set!");
                if (cfg == null)
                    Administrators = new ulong[0];
                else
                    Administrators = ((JArray)cfg).ToObject<ulong[]>();

                ValidateConfig("Environment", ref cfg, typeof(string), false, null, "");
                if (!Enum.TryParse(cfg?.ToString(), false, out KurumiEnvironment environment))
                {
                    SendWarning("Invalid environment setting! Starting in 'Development' mode.");
                    Environment = KurumiEnvironment.Development;
                }
                else
                    Environment = environment;
                if (Environment == KurumiEnvironment.Development)
                    SendWarning("Starting in 'Development' mode!");

                ValidateConfig("DefaultLanguage", ref cfg, typeof(string), false, "en", "Default language is empty! Starting with 'English (en)'.");
                DefaultLanguage = (string)cfg;

                ValidateConfig("LoggerMode", ref cfg, typeof(long), false, 0, "");
                LoggerMode = Convert.ToInt32(cfg);
                if (LoggerMode < 0 || LoggerMode > 12)
                {
                    SendWarning("Invalid logger type!");
                    LoggerMode = 0;
                }

                ValidateConfig("MongoDBUsername", ref cfg, typeof(string), false, null, "No DB username is set, ignoring password.");
                MongoDBUsername = cfg?.ToString();

                if (MongoDBUsername != null)
                {
                    ValidateConfig("MongoDBPassword", ref cfg, typeof(string), false, null, "No DB password is set, ignoring username.");
                    MongoDBPassword = cfg?.ToString();
                    if (MongoDBPassword == null)
                        MongoDBUsername = null;
                }

                ValidateConfig("MongoDBIp", ref cfg, typeof(string), false, "127.0.0.1", "No DB Ip is set, using 127.0.0.1!");
                MongoDBIp = cfg.ToString();

                ValidateConfig("MongoDBPort", ref cfg, typeof(string), false, "27017", "No DB port is set, using 27017!");
                MongoDBPort = cfg.ToString();

                ValidateConfig("MongoDBName", ref cfg, typeof(string), false, "Kurumi", "No DB name is set, using Kurumi!");
                MongoDBName = cfg.ToString();

                return true;
            }
            catch (Exception exception)
            {
                ex = exception;
                return false;
            }
        }

        private static void ValidateConfig(string Key, ref object Setting, Type Expected, bool Throw = true, object DefVal = null, string Warning = null)
        {
            object val;
            if (RawConfig.ContainsKey(Key) && (val = RawConfig[Key]) != null && !string.IsNullOrWhiteSpace(val.ToString()))
            {
                if (val?.GetType() == Expected)
                    Setting = val;
                else
                    throw new Exception($"Type '{val?.GetType().ToString() ?? "null"}' is not valid for '{Key}'");
            }
            else //Config is missing this key (outdated config?), value is null (or empty/whitespace)
            {
                if (Throw)
                    throw new Exception($"'{Key}' cannot be empty");
                Setting = DefVal;
                if (!string.IsNullOrWhiteSpace(Warning))
                    SendWarning(Warning);
            }
        }


        private static void SendWarning(string warning)
        {
            ConsoleColor c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($">>> {warning}");
            Console.ForegroundColor = c;
        }

        public enum KurumiEnvironment
        {
            Development,
            Release
        }
    }
}