using Kurumi.Services.Database.Databases;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Services.Database.Models
{
    public class GuildConfig
    {
        public string Prefix { get; set; } = CommandHandler.DEFAULT_PREFIX;
        public string Lang { get; set; } = "en";
        public string PunishmentForWord { get; set; } = "Warning";
        public string PunishmentForWarning { get; set; } = "Ban";
        public ulong WelcomeChannel { get; set; } = 0;
        public bool Ranking { get; set; } = true;
        public int MaxWarnings { get; set; } = 4;
        public float Volume { get; set; } = 100;
        public int Inc { get; set; } = (int)GuildConfigDatabase.INC_GUILD;
        public List<string> BlakclistedWords { get; set; } = new List<string>();
        public List<string> WelcomeMessages { get; set; } = new List<string>();
        public List<ulong> ColorRoles { get; set; } = new List<ulong>();
    }
}