using System.Collections.Generic;
using TimeZoneConverter;

namespace Flow.Launcher.Plugin.TimeIn
{
    public class EnrichedTimeZoneProvider
    {
        private Dictionary<string,EnrichedTimeZoneInfo> timezoneToEnriched;

        public EnrichedTimeZoneProvider()
        {
            refresh();
        }

        public void refresh()
        {
            var territoriesToTimeZones = TZConvert.GetIanaTimeZoneNamesByTerritory();

            timezoneToEnriched = new Dictionary<string, EnrichedTimeZoneInfo>();

            foreach (var territoryCode in territoriesToTimeZones.Keys)
            {
                var timeZones = territoriesToTimeZones[territoryCode];

                foreach (var ianaTimeZone in timeZones)
                {
                    timezoneToEnriched[ianaTimeZone] = 
                        new EnrichedTimeZoneInfo(
                            ianaTimeZone:ianaTimeZone,
                            territoryCode:territoryCode,
                            isSoleTerritoryTimezone: timeZones.Count == 1
                        );
                }
            }
        }

        public EnrichedTimeZoneInfo GetEnrichedTimeZone(string ianaTimeZone)
        {
            return timezoneToEnriched[ianaTimeZone];
        }

        public IReadOnlyCollection<EnrichedTimeZoneInfo> GetAll()
        {
            return timezoneToEnriched.Values;
        }
    }
}