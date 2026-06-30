using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ecs.LogTool;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Ecs;

public class ApiLogMidware
{
    private RequestDelegate _next;
    private ILogger<ApiLogMidware> _logger;
    public ApiLogMidware(RequestDelegate next, ILogger<ApiLogMidware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        context.Request.EnableBuffering(); //允许重复读Body
        using StreamReader reader = new StreamReader(context.Request.Body);
        string body = await reader.ReadToEndAsync().ConfigureAwait(false);  
        body = string.IsNullOrEmpty(body) ? "无" : body; 
        context.Request.Body.Seek(0, SeekOrigin.Begin); //指针返回到起始处         

        var queryStr = context.Request.QueryString.Value;
        queryStr = string.IsNullOrEmpty(queryStr) ? "无" : queryStr;
        queryStr = queryStr.Substring(0,1) == "?" ? queryStr.Substring(1) : queryStr;

        string requestMsg = $"收到接口调用，Path: {path}， Query: {queryStr}， Body: {body}";       
        _logger.Info(requestMsg);
        
        await _next(context);        
    }
}