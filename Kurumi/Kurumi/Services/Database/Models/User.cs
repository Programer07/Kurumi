using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Services.Database.Models
{
    public class User
    {
        public ObjectId Id { get; set; }
        public ulong UserId { get; set; }
        public uint Exp { get; set; } = 0;
        public uint Credit { get; set; } = 500;
    }
}