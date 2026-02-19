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

        public Task InitAsync(PluginInitContext context)
        {
            _context = context;

            _settings = _context.API.LoadSettingJsonStorage<Settings>();

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _mainActionKeyword = _context.CurrentPluginMetadata.ActionKeyword;

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
                    Title = $"{savedTimezone.IanaTimeZone} - {dateTime:HH:mm}",
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
            
            List<Region> regions = await GetTimezoneCountryMappings(token);

            token.ThrowIfCancellationRequested();

            foreach (var region in regions)
            {
                var timezone = region.TimeZone;
                var city = timezone.Split("/").Last().Replace("_"," ");

                var newName = $"{region.CountryName} - {city}";

                if (! newName.ToLower().Contains(filter)) continue;

                var savedTimezone = new SavedTimezoneItem(
                    ianaTimeZone:timezone
                );

                results.Add(new Result{
                    Title = newName,
                    Action =  _ =>
                    {
                        _settings.SavedTimezones.Add(savedTimezone);
                        _context.API.SaveSettingJsonStorage<Settings>();

                        _context.API.ChangeQuery(_mainActionKeyword);
                        return false;
                    }
                }); 
            }

            return results;
        }

       

        

        public record Region(
            string ContinentCode,
            string CountryCode,
            string CountryName,
            string TimeZone
        );

        public async Task<List<Region>> GetTimezoneCountryMappings(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var single_timezone_exceptions = new Dictionary<string,string>{
                ["China"] = "Asia/Shanghai",
                ["Kazakhstan"] = "Asia/Almaty",
                ["Argentina"] = "America/Argentina/Buenos_Aires",
                ["Uzbekistan"] = "Asia/Samarkand"
            };

            const string url = "https://raw.githubusercontent.com/bxparks/tzplus/refs/heads/master/data/country_timezones.txt";

            var response = await _httpClient.GetAsync(url,token);
            response.EnsureSuccessStatusCode();

            token.ThrowIfCancellationRequested();

            var responseBody = await response.Content.ReadAsStringAsync(token);
            
            var regions = new List<Region>();

            using var reader = new StringReader(responseBody);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                // Skip blank lines
                if (string.IsNullOrEmpty(line))
                    continue;

                // Skip comments
                if (line.StartsWith("#"))
                    continue;

                // Remove inline comments
                var hashIndex = line.IndexOf('#');
                if (hashIndex >= 0)
                    line = line[..hashIndex].Trim();

                var parts = line.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                );

                if (parts.Length < 3)
                    continue; // malformed line

                var name = CountryCodeConverter.GetCountryName(parts[1]);
                var timezone = parts[2];

                // skip extra city timezones for countries that should only have 1
                if (
                    name != null
                    &&
                    single_timezone_exceptions.ContainsKey(name) 
                    && 
                    single_timezone_exceptions[name] != timezone
                ) continue;

                regions.Add(new Region(
                    parts[0],
                    parts[1],
                    name,
                    timezone
                ));
            }

            return regions;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            var results = new List<Result>();

            switch (selectedResult.ContextData)
            {
                case SavedTimezoneItem savedTimezoneItem:
                {
                    
                    results.Add(new Result
                    {
                        Title = "Delete",
                        SubTitle = "Delete this timezone item",
                        Glyph = new GlyphInfo("sans-serif","X"),
                        Action = _ =>
                        {
                            _settings.SavedTimezones.Remove(savedTimezoneItem);
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