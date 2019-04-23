using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Services.Database.Models
{
    public class Reward
    {
        public Dictionary<int, ulong> Rewards { get; set; } = new Dictionary<int, ulong>();
    }
}