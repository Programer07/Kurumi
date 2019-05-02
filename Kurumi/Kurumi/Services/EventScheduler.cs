using Kurumi.Modules.Utility;
using Kurumi.Services.Database;
using Kurumi.Services.Random;
using Kurumi.StartUp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Kurumi.Services
{
    public class EventScheduler : IDisposable
    {
        private static readonly Dictionary<EventType, Action<Event>> Map = new Dictionary<EventType, Action<Event>>()
        {
            { EventType.StatusUpdate, (e) => Program.Bot.NextPlayingStatus() },
            { EventType.ESSave, Current.Save },
            { EventType.DBSave, (e) => DatabaseManager.SaveDatabases(false) },
            { EventType.Reminder, UserCommands.SendReminder },
        };

        private static EventScheduler _Current;
        public static EventScheduler Current
        {
            get
            {
                if (_Current == null)
                    _Current = new EventScheduler();
                return _Current;
            }
        }


        public List<Event> Events;
        private readonly Timer Clock;

        public EventScheduler(bool Load = true)
        {
            Events = new List<Event>();
            if (Load)
                this.Load();
            Clock = new Timer()
            {
                SynchronizingObject = null,
                Interval = 3000
            };
            Clock.Elapsed += (o, args) => CheckElapsed();
            Clock.Start();
        }

        public Task CheckElapsed()
        {
            return Task.Run(() =>
            {
                for (int i = 0; i < Events.Count; i++)
                {
                    var e = Events[i];
                    if (e.AllowRun)
                    {
                        Run(e);
                    }
                }
                return Task.CompletedTask;
            });
        }
        public Task Run(Event e)
        {
            return Task.Run(() => 
            {
                var f = Map[e.Type];
                e.SetRunning();
                try
                {
                    f.Invoke(e);
                    if (e.Finish())
                        Events.Remove(e);
                }
                catch (Exception ex)
                {
                    e.State = EventState.FAILED;
                    e.Error = ex;
                }
                return Task.CompletedTask;
            });
        }
        private void Load()
        {
            string Path = $"{KurumiPathConfig.Data}Events.json";
            if (File.Exists(Path))
            {
                string Content = File.ReadAllText(Path);
                Events = JsonConvert.DeserializeObject<List<Event>>(Content);
            }
        }
        public void Save(Event e = null)
        {
            Clock.Stop();
            try
            {
                var Events = new List<Event>();
                var FailedEvents = new List<Event>();
                for (int i = 0; i < this.Events.Count; i++)
                {
                    var Event = this.Events[i];
                    if (!Event.Write)
                        continue;
                    if (Event.State == EventState.FAILED)
                        FailedEvents.Add(Event);
                    else
                        Events.Add(Event);
                }

                string Content = JsonConvert.SerializeObject(Events, Formatting.Indented);
                File.WriteAllText(KurumiPathConfig.Data + "Events.json", Content);
                if(FailedEvents.Count > 0 )
                {
                    Content = JsonConvert.SerializeObject(FailedEvents, Formatting.Indented);
                    File.WriteAllText(KurumiPathConfig.Data + "FailedEvents.json", Content);
                }
            }
            catch (Exception ex)
            {
                if (e != null)
                {
                    e.Error = ex;
                    e.State = EventState.FAILED;
                }
                else
                {
                    Clock.Start();
                    throw ex; //If this wasn't called by the event scheduler (e == null) I want the exception to be thrown after the clock is started again.
                }
            }
            Clock.Start();
        }
        public void Add(Event e)
        {
            var rng = new KurumiRandom();
            GetNew:
            int Id = rng.Next(100, 1000);
            for (int i = 0; i < Events.Count; i++)
            {
                if (Events[i].Id == Id)
                    goto GetNew;
            }
            e.Id = Id;
            Events.Add(e);
        }
        public void Stop() => Clock.Stop();
        public void Start() => Clock.Start();
        public void Dispose() => Clock.Dispose();
        public override string ToString()
        {
            if (Events.Count == 0)
                return "No events are scheduled!";

            string Text = "```|   Id   |   Status   |    Type    |  Fail  |      Name      |\n--------------------------------------------------------------\n"/*|        |            |            |        |\n"*/;

            for (int i = 0; i < Events.Count; i++)
            {
                var t = Events[i];

                double IdSides = (double)(8 - t.Id.ToString().Length) / 2;
                double StateSides = (double)(12 - t.State.ToString().Length) / 2;
                double TypeSides = (double)(12 - t.Type.ToString().Length) / 2;
                double ErrorSides = (double)(8 - (t.Error != null).ToString().Length) / 2;
                double NameSides = (double)(16 - t.Name.Length) / 2;

                Text += $"|{GetWhiteSpaces((int)Math.Ceiling(IdSides))}{t.Id}{GetWhiteSpaces((int)Math.Floor(IdSides))}" +
                            $"|{GetWhiteSpaces((int)Math.Ceiling(StateSides))}{t.State}{GetWhiteSpaces((int)Math.Floor(StateSides))}" +
                            $"|{GetWhiteSpaces((int)Math.Ceiling(TypeSides))}{t.Type}{GetWhiteSpaces((int)Math.Floor(TypeSides))}" +
                            $"|{GetWhiteSpaces((int)Math.Ceiling(ErrorSides))}{t.Error != null}{GetWhiteSpaces((int)Math.Floor(ErrorSides))}" +
                            $"|{GetWhiteSpaces((int)Math.Ceiling(NameSides))}{t.Name}{GetWhiteSpaces((int)Math.Floor(NameSides))}|\n";
            }
            Text += "```";
            return Text;
        }
        private string GetWhiteSpaces(int count) => new string(' ', count);
    }

    public class Event
    {
        [JsonIgnore]
        public EventState State { get; set; }
        [JsonIgnore]
        public Exception Error { get; set; }
        [JsonIgnore]
        public int Id { get; set; }
        [JsonIgnore]
        public bool Write { get; set; }

        public DateTime Run { get; set; }
        public EventType Type { get; set; }
        public TimeSpan Interval { get; set; }
        public int Remaining { get; set; }
        public bool AllowParallel { get; set; }
        public bool IgnoreFail { get; set; }
        public ulong[] Ids { get; set; }
        public string Name { get; set; }
        public object Data { get; set; }

        public void SetRunning()
        {
            Run = DateTime.Now + Interval;
            if (Remaining != -1)
            {
                Remaining--;
                if (Remaining <= 0)
                    Remaining = 0;
            }
            State = EventState.RUNNING;
        }
        public bool Finish()
        {
            if (Remaining != 0)
            {
                State = EventState.WAITING;
                return false;
            }
            else
                return true;
        }

        public Event(ulong[] Ids, EventType Type, int RunTimes, DateTime Run, TimeSpan Interval, object Data, bool AllowParallel, string Name, bool Write, bool IgnoreFail)
        {
            this.Write = Write;
            this.Ids = Ids;
            this.Type = Type;
            this.Remaining = RunTimes;
            this.Run = Run;
            this.Interval = Interval;
            this.Data = Data;
            this.AllowParallel = AllowParallel;
            this.Name = Name;
            this.State = EventState.WAITING;
            this.IgnoreFail = IgnoreFail;
        }

        [JsonConstructor]
        public Event(ulong[] Ids, string Type, int Remaining, DateTime Run, string Interval, object Data, bool AllowParallel, string Name, bool IgnoreFail)
        {
            Write = true;
            this.Ids = Ids;
            this.Remaining = Remaining;
            this.Run = Run;
            this.Data = Data;
            this.AllowParallel = AllowParallel;
            this.Name = Name;
            this.State = EventState.WAITING;
            this.IgnoreFail = IgnoreFail;

            if (!Enum.TryParse(Type, out EventType type))
            {
                State = EventState.FAILED;
                Error = new Exception($"Bad type: '{Type}'");
                this.IgnoreFail = false;
            }
            else
                this.Type = type;

            if (Remaining == 0)
            {
                this.Type = EventType.Invalid;
                State = EventState.FAILED;
                Error = new Exception("Invalid number: 'Remaining'");
            }

            if (!TimeSpan.TryParse(Interval, out TimeSpan ts))
            {
                State = EventState.FAILED;
                Error = new Exception("Bad time");
                this.IgnoreFail = false;
            }
            else
                this.Interval = ts;
        }

        [JsonIgnore]
        public bool AllowRun => (Error == null || IgnoreFail)
                                && (State != EventState.RUNNING || AllowParallel)
                                && (Remaining > 0 || Remaining == -1)
                                && Run <= DateTime.Now;
    }

    public enum EventState
    {
        WAITING,
        RUNNING,
        FAILED
    }
    public enum EventType
    {
        Invalid,
        DBSave,
        ESSave,
        StatusUpdate,
        Reminder
    }
}