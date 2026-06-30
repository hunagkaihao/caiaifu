using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ecs.LogTool;
using Microsoft.Extensions.Logging;

namespace Ecs.Log;

public class LogService : EcsAppService, ILogService
{
    private readonly ILogger<LogService> _logger;

    public LogService(ILogger<LogService> logger)
    {
        _logger = logger;
    }

    public List<LogDto> query(string msgSnip, string grade, int logCntMax = 500)
    {
        try
        {
            List<SqliteLogItem> logs = SqliteLogHelper.GetLogItems(msgSnip, grade, logCntMax);
            var ret = ObjectMapper.Map<List<SqliteLogItem>, List<LogDto>>(logs);
            return ret;
        }
        catch(Exception ex)
        {
            _logger.Error(ex.Message);
            return new List<LogDto>();
        }
    }
}