using System;
using System.Linq;
using TimeZoneConverter;

namespace Flow.Launcher.Plugin.TimeIn
{
    public sealed class EnrichedTimeZoneInfo
    {
        public readonly string IanaTimeZone;
        public readonly string TerritoryName;
        public readonly string TerritoryCode;
        public readonly string SpecificLocation;
        public readonly bool IsSoleTerritoryTimezone;

        public EnrichedTimeZoneInfo(string ianaTimeZone, string territoryCode, bool isSoleTerritoryTimezone)
        {
            if(ianaTimeZone is null)
            {
                throw new ArgumentNullException("ianaTimeZone cannot be null");
            }

            IanaTimeZone = ianaTimeZone;
            SpecificLocation = ianaTimeZone.Split("/").Last().Replace("_"," ");

            TerritoryCode = territoryCode;
            TerritoryName = CountryCodeConverter.GetCountryName(territoryCode);

            IsSoleTerritoryTimezone = isSoleTerritoryTimezone;
        }
    }
}