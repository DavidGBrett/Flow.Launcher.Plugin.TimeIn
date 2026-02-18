using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.TimeIn
{
    public class TimeNowApiClient
    {
        private const string ApiBaseUrl = "https://time.now/developer/api/";
        private HttpClient _httpClient;

        private Dictionary<string,CachedValue<Dictionary<string,object>>> timezoneDictCache = new Dictionary<string, CachedValue<Dictionary<string,object>>>();
        private TimeSpan? _cacheDuration;

        public TimeNowApiClient(HttpClient httpClient, TimeSpan? cacheDuration = null)
        {
            _httpClient = httpClient;
            _cacheDuration = cacheDuration;
        }

        public async Task<List<string>> GetTimezones(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var fullUrl = ApiBaseUrl + "timezone";
            
            var response = await _httpClient.GetAsync(fullUrl,token);
            response.EnsureSuccessStatusCode();

            token.ThrowIfCancellationRequested();
            
            var responseBody = await response.Content.ReadAsStringAsync(token);

            return JsonSerializer.Deserialize<List<string>>(responseBody);
        }

        private async Task<Dictionary<string,object>> GetTimezoneDict(string timezone, CancellationToken token)
        {
            if (_cacheDuration is not null && timezoneDictCache.ContainsKey(timezone))
            {
                var cache = timezoneDictCache[timezone];
                if (! cache.IsExpired())
                {
                    return cache.Value;
                }
            }

            token.ThrowIfCancellationRequested();

            var fullUrl = $"{ApiBaseUrl}timezone/{timezone}";
            
            var response = await _httpClient.GetAsync(fullUrl,token);
            response.EnsureSuccessStatusCode();
            
            var responseBody = await response.Content.ReadAsStringAsync(token);

            token.ThrowIfCancellationRequested();

            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);

            if (_cacheDuration is not null)
            {
                timezoneDictCache[timezone] = 
                    new CachedValue<Dictionary<string,object>>(value:dict,expirationTime:_cacheDuration.Value);
            }
            return dict;
        }

         public async Task<DateTimeOffset> GetTimezoneTime(string timezone, CancellationToken token)
        {
            var dict = await GetTimezoneDict(timezone,token);

            token.ThrowIfCancellationRequested();

            var timeString = dict["datetime"].ToString();

            var dateTime = DateTimeOffset.Parse(timeString);

            return dateTime;
        }

        public async Task<TimeSpan> GetTimezoneOffset(string timezone, CancellationToken token)
        {
            var dict = await GetTimezoneDict(timezone,token);

            token.ThrowIfCancellationRequested();

            var offsetString = dict["utc_offset"].ToString();

            var offsetTime = TimeSpan.Parse(offsetString);

            return offsetTime;
        }
    }
}