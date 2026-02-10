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


        public Task InitAsync(PluginInitContext context)
        {
            _context = context;

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

            List<string> timezones = await GetTimezones(token);

            token.ThrowIfCancellationRequested();

            foreach (var timezone in timezones)
            {
                results.Add(new Result{
                    Title = timezone,
                }); 
            }

            return results;
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