using System;
using System.Linq;
using TimeZoneConverter;

namespace Flow.Launcher.Plugin.TimeIn
{
    public sealed class EnrichedTimeZoneInfo
    {
        public string IanaTimeZone {get;set;}
        public string TerritoryName {get;set;}
        public string TerritoryCode {get;set;}
        public string SpecificLocation {get;set;}
        public bool IsSoleTerritoryTimezone {get;set;}

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