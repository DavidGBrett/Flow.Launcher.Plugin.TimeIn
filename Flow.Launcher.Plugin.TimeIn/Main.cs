using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using TimeZoneConverter;


namespace Flow.Launcher.Plugin.TimeIn
{
    public class TimeIn : IAsyncPlugin, IContextMenu
    {
        private PluginInitContext _context;
        private Settings _settings;
        private HttpClient _httpClient;
        private string _mainActionKeyword;
        
        private Dictionary<string,EnrichedTimeZoneInfo> timezoneToEnriched;

        public Task InitAsync(PluginInitContext context)
        {
            _context = context;

            _settings = _context.API.LoadSettingJsonStorage<Settings>();

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _mainActionKeyword = _context.CurrentPluginMetadata.ActionKeyword;

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

            return Task.CompletedTask;
        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var results = new List<Result>();

            string addKeyword = "add-";

            if (query.Search.StartsWith(addKeyword)){
                var filter = query.Search.Substring(addKeyword.Length).ToLower();

                results = await GetAddTimezoneResults(
                    filter:filter,
                    token:token
                );
            }
            else{
                var filter = query.Search.ToLower();

                results = await GetSavedTimezonesResults(
                    filter:filter,
                    token:token
                );
            }

            return results;
        }

        private async Task<List<Result>> GetSavedTimezonesResults(string filter, CancellationToken token){
            token.ThrowIfCancellationRequested();

            var results = new List<Result>();

            foreach (var savedTimezone in _settings.SavedTimezones)
            {
                if (! savedTimezone.IanaTimeZone.ToLower().Contains(filter)) continue;

                string windowsTimeZone = TZConvert.IanaToWindows(savedTimezone.IanaTimeZone);
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZone);
                var dateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);

                results.Add(new Result{
                    Title = $"{savedTimezone.TerritoryName} - {savedTimezone.SpecificLocation}",
                    SubTitle = $"{dateTime:HH:mm}",
                    Glyph = new GlyphInfo("sans-serif",$"{dateTime:HH}"),
                    ContextData = savedTimezone
                }); 
            }

            results.Add(new Result{
                Title = "Add Timezone",
                Glyph = new GlyphInfo("sans-serif","＋"),
                Score = -100, // Low score to appear at the bottom (make sure real matches come first)
                Action = _ =>
                {
                    // Change to the add group query
                    _context.API.ChangeQuery($"{_mainActionKeyword} add-",false);
                    return false;
                }
            });

            return results;
        }


        private async Task<List<Result>> GetAddTimezoneResults(string filter, CancellationToken token){
            token.ThrowIfCancellationRequested();

            var results = new List<Result>();
            
            token.ThrowIfCancellationRequested();

            foreach (var tzInfo in timezoneToEnriched.Values)
            {
                var title = $"{tzInfo.TerritoryName} - {tzInfo.SpecificLocation}";
                var SubTitle = $"{tzInfo.TerritoryCode} - {tzInfo.IanaTimeZone}";

                if (! title.ToLower().Contains(filter)) continue;

                results.Add(new Result{
                    Title = title,
                    SubTitle = SubTitle,
                    Action =  _ =>
                    {
                        _settings.SavedTimezones.Add(tzInfo);
                        _context.API.SaveSettingJsonStorage<Settings>();

                        _context.API.ChangeQuery(_mainActionKeyword);
                        return false;
                    }
                }); 
            }

            return results;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var results = new List<Result>();

            switch (selectedResult.ContextData)
            {
                case EnrichedTimeZoneInfo savedTimezone:
                {
                    
                    results.Add(new Result
                    {
                        Title = "Delete",
                        SubTitle = "Delete this timezone item",
                        Glyph = new GlyphInfo("sans-serif","X"),
                        Action = _ =>
                        {
                            _settings.SavedTimezones.Remove(savedTimezone);
                            _context.API.SaveSettingJsonStorage<Settings>();
                            _context.API.ReQuery();

                            return false;
                        }
                    });

                    break;
                }
            }
            
            return results;
        }
    }
}