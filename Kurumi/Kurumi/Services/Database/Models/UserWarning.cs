using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Services.Database.Models
{
    public class UserWarning
    {
        public ulong UserId { get; set; }
        public int Count { get; set; }
    }
}