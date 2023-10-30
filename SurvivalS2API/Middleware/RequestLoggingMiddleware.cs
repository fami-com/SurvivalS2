using System.Text;
using Microsoft.Extensions.Options;

namespace SurvivalS2API.Middleware;

public class RequestLoggingOptions
{
    public bool LogQuery { get; set; }
    public bool LogBody { get; set; }
    
    public RequestLoggingOptions(bool logBody, bool logQuery)
    {
        LogQuery = logQuery;
        LogBody = logBody;
    }
}

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly bool _logBody;
    private readonly bool _logQuery;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IOptions<RequestLoggingOptions> options)
    {
        _next = next;
        _logger = logger;
        _logBody = options.Value.LogBody;
        _logQuery = options.Value.LogQuery;
    }

    public async Task Invoke(HttpContext context)
    {
        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
        var path = context.Request.Path;
        var method = context.Request.Method;
        
        _logger.Log(LogLevel.Information, "[{Time}] REQUEST {Method} {Path}", time, method, path);

        if (_logQuery)
        {
            var queryBuilder = new List<string>(context.Request.Query.Count);
            foreach (var (k, v) in context.Request.Query)
            {
                queryBuilder.Add($"{k}={v}");
            }
            var query = queryBuilder.Count > 0 ? '?' + string.Join('&', queryBuilder) : "";
            
            if (!string.IsNullOrEmpty(query)) _logger.Log(LogLevel.Information, "[{Time}] {Query}", time, query);
        }
        
        if (_logBody)
        {
            context.Request.EnableBuffering();
            var body = await ReadRequestBody(context.Request.Body);
            context.Request.Body.Seek(0, SeekOrigin.Begin);
            
            if (!string.IsNullOrEmpty(body)) _logger.Log(LogLevel.Information, "[{Time}] {Body}", time, body);
        }

        await _next(context);
    }

    private async Task<string> ReadRequestBody(Stream body)
    {
        using var reader = new StreamReader(body, Encoding.UTF8, true, 1024, true);
        return await reader.ReadToEndAsync();
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder, bool logBody, bool logQuery)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>(Options.Create(new RequestLoggingOptions(logBody, logQuery)));
    }
}