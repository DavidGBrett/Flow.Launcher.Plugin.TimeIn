using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Flow.Launcher.Plugin.TimeIn
{
    public class Settings : BaseModel
    {
        public ObservableCollection<EnrichedTimeZoneInfo> SavedTimezones {set;get;} = new ObservableCollection<EnrichedTimeZoneInfo>();
    }
}