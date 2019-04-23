using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class RequireGuildOwnerVoteAttribute : Attribute
    {
        public RequireGuildOwnerVoteAttribute() { }
    }
}