using System.Collections.Generic;
using Ecs.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Ecs.Log;

[Route("ecs/log")]
[ApiController]
public class LogController : EcsController, ILogService
{
    private readonly ILogService _logServece;
    public LogController(ILogService logService)
    {
        _logServece = logService;    
    }

    [HttpGet("query")]
    public List<LogDto> query(string msgSnip, string grade, int logCntMax = 500)
    {
        return _logServece.query(msgSnip, grade, logCntMax);
    }
}