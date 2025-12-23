using LoggingService.Services;
using LoggingService.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace LoggingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly FileLogStorage _storage;

        public LogsController(FileLogStorage storage)
        {
            _storage = storage;
        }

        [HttpPost]
        public async Task<IActionResult> WriteLog([FromBody] LogEntry logEntry)
        {
            if (string.IsNullOrEmpty(logEntry.Application) || string.IsNullOrEmpty(logEntry.Message))
            {
                return BadRequest("Application and Message are required");
            }

            await _storage.WriteLogAsync(logEntry);
            return Ok();
        }

        [HttpPost("batch")]
        public async Task<IActionResult> WriteLogBatch([FromBody] List<LogEntry> logEntries)
        {
            foreach (var log in logEntries)
            {
                if (!string.IsNullOrEmpty(log.Application) && !string.IsNullOrEmpty(log.Message))
                {
                    await _storage.WriteLogAsync(log);
                }
            }
            return Ok();
        }

        [HttpPost("query")]
        public async Task<ActionResult<LogQueryResponse>> QueryLogs([FromBody] LogQueryRequest request)
        {
            var response = await _storage.QueryLogsAsync(request);
            return Ok(response);
        }
    }
}
