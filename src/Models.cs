using System;
using System.Collections.Generic;

namespace CountdownApp
{
    public class CountdownEvent
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Date { get; set; }
        public DateTime CreatedAt { get; set; }

        public CountdownEvent()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = string.Empty;
            Date = string.Empty;
            CreatedAt = DateTime.Now;
        }
    }

    public class AppState
    {
        public int Version { get; set; }
        public bool Initialized { get; set; }
        public List<CountdownEvent> Events { get; set; }
        public DateTime UpdatedAt { get; set; }

        public AppState()
        {
            Version = 1;
            Initialized = false;
            Events = new List<CountdownEvent>();
            UpdatedAt = DateTime.Now;
        }

        public void Normalize()
        {
            if (Events == null) Events = new List<CountdownEvent>();

            for (int i = Events.Count - 1; i >= 0; i--)
            {
                CountdownEvent item = Events[i];
                if (item == null)
                {
                    Events.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrEmpty(item.Id)) item.Id = Guid.NewGuid().ToString("N");
                if (item.Name == null) item.Name = string.Empty;
                if (item.Date == null) item.Date = string.Empty;
            }
        }
    }
}
