using System.Linq;
using TimeZoneConverter;

namespace Flow.Launcher.Plugin.TimeIn
{
    public readonly struct EnrichedTimeZoneInfo
    {
        public readonly string IanaTimeZone;
        public readonly string TerritoryName;
        public readonly string TerritoryCode;
        public readonly string SpecificLocation;
        public readonly bool IsSoleTerritoryTimezone;

        public EnrichedTimeZoneInfo(string ianaTimeZone, string territoryCode, bool isSoleTerritoryTimezone)
        {
            IanaTimeZone = ianaTimeZone;
            SpecificLocation = ianaTimeZone.Split("/").Last().Replace("_"," ");

            TerritoryCode = territoryCode;
            TerritoryName = CountryCodeConverter.GetCountryName(territoryCode);

            IsSoleTerritoryTimezone = isSoleTerritoryTimezone;
        }
    }
}