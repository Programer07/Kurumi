using Discord;
using Discord.Commands;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Kurumi.Services.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Misc
{
    public class Jobs : ModuleBase
    {
        [Command("jobs")]
        public async Task SendJobs([Remainder, Optional]string job)
        {
            try
            {
                var lang = Language.GetLanguage(Context.Guild);
                List<Job> jobs = JsonConvert.DeserializeObject<List<Job>>(File.ReadAllText(KurumiPathConfig.Data + "Jobs.json"));
                if (job == null) //Send all jobs
                {
                    string JobsString = string.Empty;
                    for (int i = 0; i < jobs.Count; i++)
                    {
                        Job j = jobs[i];
                        JobsString += $"{i + 1}) **{j.Title}**\n{j.ShortDescription}\n";
                    }

                    await Context.Channel.SendEmbedAsync(lang["jobs_intro"] + "\n\n" + JobsString + "\n" + lang["jobs_footer"]);
                }
                else
                {
                    Job j = jobs.FirstOrDefault(x => x.Title.StartsWith(job, StringComparison.CurrentCultureIgnoreCase));
                    if (j == null)
                    {
                        await Context.Channel.SendEmbedAsync(lang["jobs_not_found"]);
                        return;
                    }
                    await Context.Channel.SendEmbedAsync(j.Description, Title: j.Title);
                }
                await Utilities.Log(new LogMessage(LogSeverity.Info, "Jobs", "success"), Context);
            }
            catch (Exception ex)
            {
                await Utilities.Log(new LogMessage(LogSeverity.Error, "Jobs", null, ex), Context);
            }
        }

        public class Job
        {
            public string Title { get; set; }
            public string ShortDescription { get; set; }
            public string Description { get; set; }
        }
    }
}