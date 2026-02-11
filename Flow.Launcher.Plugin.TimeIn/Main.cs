using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;


namespace Flow.Launcher.Plugin.TimeIn
{
    public class TimeIn : IAsyncPlugin
    {
        private PluginInitContext _context;
        private HttpClient _httpClient;
        private const string ApiBaseUrl = "https://time.now/developer/api/";

        private List<string> _savedTimezones;

        public Task InitAsync(PluginInitContext context)
        {
            _context = context;

            _savedTimezones = new List<string>{
                "Asia/Shanghai"
            };

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            return Task.CompletedTask;
        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var results = new List<Result>{};

            foreach (var timezone in _savedTimezones)
            {
                var dateTime = await GetTimezoneTime(timezone,token);

                results.Add(new Result{
                    Title = $"{timezone} - {dateTime:HH:mm}",
                }); 
            }

            return results;
        }

        public async Task<DateTimeOffset> GetTimezoneTime(string timezone, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var fullUrl = $"{ApiBaseUrl}timezone/{timezone}";
            
            var response = await _httpClient.GetAsync(fullUrl,token);
            response.EnsureSuccessStatusCode();
            
            var responseBody = await response.Content.ReadAsStringAsync(token);


            token.ThrowIfCancellationRequested();

            using var doc = JsonDocument.Parse(responseBody);

            var timeString = doc.RootElement.GetProperty("datetime").GetString();


            var dateTime = DateTimeOffset.Parse(timeString);

            return dateTime;
            
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
    }
}