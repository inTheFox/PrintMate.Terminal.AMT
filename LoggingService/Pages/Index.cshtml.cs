using Microsoft.AspNetCore.Mvc.RazorPages;
using LoggingService.Services;
using LoggingService.Shared.Models;
using LogLevel = LoggingService.Shared.Models.LogLevel;

namespace LoggingService.Pages
{
    public class IndexModel : PageModel
    {
        private readonly FileLogStorage _logStorage;

        public List<LogEntry>? Logs { get; set; }
        public int TotalCount { get; set; }
        public string? Application { get; set; }
        public int? MinLevel { get; set; }
        public string? SearchText { get; set; }
        public int Take { get; set; } = 100;

        public IndexModel(FileLogStorage logStorage)
        {
            _logStorage = logStorage;
        }

        public async Task OnGetAsync(string? application, int? minLevel, string? search, int take = 100)
        {
            Application = application;
            MinLevel = minLevel;
            SearchText = search;
            Take = take;

            var request = new LogQueryRequest
            {
                Application = application,
                MinLevel = minLevel.HasValue ? (LogLevel)minLevel.Value : null,
                SearchText = search,
                StartDate = DateTime.Now.AddDays(-7),
                EndDate = DateTime.Now,
                Skip = 0,
                Take = take
            };

            var response = await _logStorage.QueryLogsAsync(request);
            Logs = response.Logs;
            TotalCount = response.TotalCount;
        }
    }
}
