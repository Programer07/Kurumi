using Discord;
using Kurumi.Services.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kurumi.Common
{
    public class KurumiData
    {
        public string Version { get; set; }
        public string ApiVersion { get; set; }
        public List<string> PremiumServers { get; set; } = new List<string>();

        public static KurumiData Get()
        {
            string Path = KurumiPathConfig.Data + "KurumiData.json";
            if (!File.Exists(Path))
            {
                Utilities.Log(new LogMessage(LogSeverity.Warning, "KurumiData", "File not found!"));
                return new KurumiData();
            }

            string Content = File.ReadAllText(Path);
            return JsonConvert.DeserializeObject<KurumiData>(Content);
        }
    }
}