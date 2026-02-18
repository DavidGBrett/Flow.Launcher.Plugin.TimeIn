using System;
using System.Collections.Generic;
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

        public TimeNowApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
    }
}