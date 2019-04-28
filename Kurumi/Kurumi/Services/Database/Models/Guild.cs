using Kurumi.Services.Database.Databases;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Services.Database.Models
{
    public class Guild
    {
        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public string Prefix { get; set; } = "!k.";
        public string Lang { get; set; } = "en";
        public string PunishmentForWord { get; set; } = "Warning";
        public string PunishmentForWarning { get; set; } = "Ban";
        public ulong WelcomeChannel { get; set; } = 0;
        public bool Ranking { get; set; } = true;
        public uint MaxWarnings { get; set; } = 4;
        public float Volume { get; set; } = 100;
        public int Increment { get; set; } = (int)GuildDatabase.INC_GUILD;
        public List<string> WelcomeMessages { get; set; } = new List<string>();
        public List<string> BlacklistedWords { get; set; } = new List<string>();
        public List<ulong> ColorRoles { get; set; } = new List<ulong>();
        public List<Reward> Rewards { get; set; } = new List<Reward>();
        public List<GUser> Users { get; set; } = new List<GUser>();
        public List<RolePermissions> Permissions { get; set; } = new List<RolePermissions>();
        public List<DeletedMessage> Messages { get; set; } = new List<DeletedMessage>();

        [BsonIgnore] //Dictionary can only be used if the key is string, on load the contents of 'Users' will be copied into this and back on saving
        public ConcurrentDictionary<ulong, GUser> _Users { get; set; } = new ConcurrentDictionary<ulong, GUser>();
    }

    public class GUser
    {
        public ulong UserId { get; set; }
        public uint Exp { get; set; } = 0;
        public string AfkMessage { get; set; } = null;
        public uint Warnings { get; set; } = 0;
    }
    public class Reward
    {
        public uint Level { get; set; }
        public ulong Role { get; set; }
    }
    public class RolePermissions
    {
        public ulong RoleId { get; set; }
        public List<string> PermissionList { get; set; }
    }
    public class DeletedMessage
    {
        public ulong MessageId { get; set; }
        public string Text { get; set; }
        public DateTime SentAt { get; set; }
        public string SentBy { get; set; }
    }
}