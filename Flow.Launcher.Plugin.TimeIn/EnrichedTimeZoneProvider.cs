using System;
using System.Collections.Generic;
using TimeZoneConverter;

namespace Flow.Launcher.Plugin.TimeIn
{
    public class EnrichedTimeZoneProvider
    {
        private Dictionary<string,EnrichedTimeZoneInfo> timezoneToEnriched;
        private Func<DateTime> _currentTimeProvider;
        private DateTime _lastRefreshTime;
        private readonly TimeSpan? _refreshInterval;

        public EnrichedTimeZoneProvider(
            TimeSpan refreshInterval,
            Func<DateTime> currentTimeProvider = null
        ){
            
            _currentTimeProvider = currentTimeProvider is null ? ()=>DateTime.UtcNow : currentTimeProvider;

            _refreshInterval = refreshInterval;

            refresh();
        }

        private bool isExpired() => _currentTimeProvider() - _lastRefreshTime > _refreshInterval;

        private void RefreshIfExpired()
        {
            if (isExpired())
            {
                refresh();
            }
        }

        public void refresh()
        {
            if (! isExpired()) return;

            var territoriesToTimeZones = TZConvert.GetIanaTimeZoneNamesByTerritory();

            var newDict = new Dictionary<string, EnrichedTimeZoneInfo>();

            foreach (var territoryCode in territoriesToTimeZones.Keys)
            {
                var timeZones = territoriesToTimeZones[territoryCode];

                foreach (var ianaTimeZone in timeZones)
                {
                    newDict[ianaTimeZone] = 
                        new EnrichedTimeZoneInfo(
                            ianaTimeZone:ianaTimeZone,
                            territoryCode:territoryCode,
                            isSoleTerritoryTimezone: timeZones.Count == 1
                        );
                }
            }

            timezoneToEnriched = newDict;
            _lastRefreshTime = _currentTimeProvider();
        }

        public EnrichedTimeZoneInfo GetEnrichedTimeZone(string ianaTimeZone)
        {
            RefreshIfExpired();
            return timezoneToEnriched[ianaTimeZone];
        }

        public IReadOnlyCollection<EnrichedTimeZoneInfo> GetAll()
        {
            RefreshIfExpired();
            return timezoneToEnriched.Values;
        }
    }
}