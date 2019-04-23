using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Kurumi.Services.Permission
{
    public class Command
    {
        public string Category { get; set; }
        [JsonProperty("Command")]
        public string CommandName { get; set; }
        public string Usage { get; set; }
        public string Alias { get; set; }
        public string Description { get; set; }
        public bool HasPermission { get; set; }
    }
}