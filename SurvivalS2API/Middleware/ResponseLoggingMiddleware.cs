using System.Net;
using Microsoft.Extensions.Options;

namespace SurvivalS2API.Middleware;

public class ResponseLoggingOptions
{
    public bool LogBody { get; set; }
    
    public ResponseLoggingOptions(bool logBody)
    {
        LogBody = logBody;
    }

    public override string ToString()
    {
        return $"{nameof(LogBody)}: {LogBody}";
    }
}

public class ResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseLoggingMiddleware> _logger;
    private readonly bool _logBody;

    public ResponseLoggingMiddleware(RequestDelegate next, ILogger<ResponseLoggingMiddleware> logger,
        IOptions<ResponseLoggingOptions> options)
    {
        Console.WriteLine(options);
        _next = next;
        _logger = logger;
        _logBody = options.Value.LogBody;
    }

    public async Task InvokeWithBody(HttpContext context)
    {
        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
        
        var originalBody = context.Response.Body;
        var path = context.Request.Path;
        
        using var newBody = new MemoryStream();
        context.Response.Body = newBody;
        
        try
        {
            await _next(context);
        }
        finally
        {
            var code = context.Response.StatusCode;
            var codeText = ((HttpStatusCode)code).ToString();
            
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var bodyReader = new StreamReader(context.Response.Body);
            var body = await bodyReader.ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            _logger.Log(LogLevel.Information, "[{Time}] RESPONSE {Code} {Description} {Path}", time, code, codeText, path);
            if (!string.IsNullOrEmpty(body)) _logger.Log(LogLevel.Information, "[{Time}] {Body}", time, body);

            await newBody.CopyToAsync(originalBody);
        }
    }
    
    public async Task InvokeWithoutBody(HttpContext context)
    {
        var path = context.Request.Path;

        await _next(context);
            
        var timeRes = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
        var code = context.Response.StatusCode;
        var codeText = ((HttpStatusCode)code).ToString();
        
        _logger.Log(LogLevel.Information, "[{Time}] RESPONSE {Code} {Description} {Path}", timeRes, code, codeText, path);
    }
    
    public async Task Invoke(HttpContext context)
    {
        if (_logBody) await InvokeWithBody(context);
        else await InvokeWithoutBody(context);
    }
}

public static class ResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseResponseLogging(this IApplicationBuilder builder, bool logBody)
    {
        return builder.UseMiddleware<ResponseLoggingMiddleware>(Options.Create(new ResponseLoggingOptions(logBody)));
    }
}