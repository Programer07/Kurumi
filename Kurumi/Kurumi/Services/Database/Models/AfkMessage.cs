using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Services.Database.Models
{
    public class AfkMessage
    {
        public ulong User { get; set; }
        public string Message { get; set; }
    }
}