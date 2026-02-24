using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Flow.Launcher.Plugin.TimeIn
{
    public class Settings : BaseModel
    {
        public HashSet<string> SavedTimeZones {set;get;} = new HashSet<string>();
    }
}