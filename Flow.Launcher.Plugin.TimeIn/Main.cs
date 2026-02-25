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
        private string _mainActionKeyword;
        private EnrichedTimeZoneProvider enrichedTZProvider;

        public Task InitAsync(PluginInitContext context)
        {
            _context = context;

            _settings = _context.API.LoadSettingJsonStorage<Settings>();

            _mainActionKeyword = _context.CurrentPluginMetadata.ActionKeyword;

            enrichedTZProvider = new EnrichedTimeZoneProvider(
                TimeSpan.FromMinutes(1)
            );

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

        private (string title, string subTitle, GlyphInfo glyph) 
        FormatTimeZoneDisplayInfo(EnrichedTimeZoneInfo enrichedTimeZone, DateTime timeZoneTime)
        {
            return (
                title: $"{enrichedTimeZone.TerritoryName} - {enrichedTimeZone.SpecificLocation}",
                subTitle: $"{timeZoneTime:HH:mm}",
                glyph: new GlyphInfo("sans-serif", $"{timeZoneTime:HH}")
            );
        }

        private DateTime GetTimeZoneTime(EnrichedTimeZoneInfo enrichedTimezone)
        {
            string windowsTimeZone = TZConvert.IanaToWindows(enrichedTimezone.IanaTimeZone);
            TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZone);
            DateTime timeZoneTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);

            return timeZoneTime;
        }

        private async Task<List<Result>> GetSavedTimezonesResults(string filter, CancellationToken token){
            token.ThrowIfCancellationRequested();

            var results = new List<Result>();

            foreach (var ianaTimeZone in _settings.SavedTimeZones)
            {
                var enrichedTimezone = enrichedTZProvider.GetEnrichedTimeZone(ianaTimeZone);

                var dateTime = GetTimeZoneTime(enrichedTimezone:enrichedTimezone);
                var (title, subTitle, glyph) = FormatTimeZoneDisplayInfo(
                    enrichedTimeZone:enrichedTimezone,
                    timeZoneTime:dateTime
                );

                if (! title.ToLower().Contains(filter)) continue;

                results.Add(
                    new Result
                    {
                        Title = title,
                        SubTitle = subTitle,
                        Glyph = glyph,
                        ContextData = enrichedTimezone
                    }
                ); 
            }

            results.Add(new Result{
                Title = "Add Timezone",
                Glyph = new GlyphInfo("sans-serif","＋"),
                Score = 100, // High score so it appears at the top of results
                Action = _ =>
                {
                    // Change to the add group query
                    _context.API.ChangeQuery($"{_mainActionKeyword} add-{filter}",false);
                    return false;
                }
            });

            return results;
        }


        private async Task<List<Result>> GetAddTimezoneResults(string filter, CancellationToken token){
            token.ThrowIfCancellationRequested();

            var results = new List<Result>();
            
            token.ThrowIfCancellationRequested();

            foreach (var tzInfo in enrichedTZProvider.GetAll())
            {
                // skip if already saved
                if (_settings.SavedTimeZones.Contains(tzInfo.IanaTimeZone))
                {
                    continue;
                }

                try{
                    var timeZoneTime = GetTimeZoneTime(enrichedTimezone:tzInfo);

                    var (title, subTitle, glyph) = FormatTimeZoneDisplayInfo(
                        enrichedTimeZone:tzInfo,
                        timeZoneTime:timeZoneTime
                    );

                    if (! title.ToLower().Contains(filter)) continue;

                    results.Add(new Result{
                        Title = title,
                        SubTitle = subTitle,
                        Glyph = glyph,
                        Action =  _ =>
                        {
                            _settings.SavedTimeZones.Add(tzInfo.IanaTimeZone);
                            _context.API.SaveSettingJsonStorage<Settings>();
                    
                            _context.API.ChangeQuery(_mainActionKeyword);
                            return false;
                        }
                    }); 
                }
                catch (InvalidTimeZoneException)
                {
                    continue;
                }
                
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
                        Glyph = new GlyphInfo("sans-serif"," X"),
                        Action = _ =>
                        {
                            _settings.SavedTimeZones.Remove(savedTimezone.IanaTimeZone);
                            _context.API.SaveSettingJsonStorage<Settings>();
                            _context.API.ReQuery();

                            return false;
                        }
                    });

                    results.Add(new Result
                    {
                        Title = "Copy IANA timezone",
                        SubTitle = $"{savedTimezone.IanaTimeZone}",
                        Glyph = new GlyphInfo("sans-serif","📋"),
                        Action = _ =>
                        {
                            _context.API.CopyToClipboard(savedTimezone.IanaTimeZone);
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