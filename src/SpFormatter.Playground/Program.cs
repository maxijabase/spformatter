using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using SpFormatter;
using SpFormatter.Playground.Models;
using SpModernizer;

const int MaxSourceBytes = 256 * 1024;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("format", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaxSourceBytes + 64 * 1024;
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();

app.MapGet("/api/health", () =>
{
    try
    {
        using var parser = new SourcePawnParser();
        using var tree = parser.ParseSource("public void OnPluginStart() {}");
        if (tree?.RootNode == null)
            return Results.Json(new { ok = false, error = "Parser returned no tree" }, statusCode: 503);

        var version = typeof(SourcePawnFormatter).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? typeof(SourcePawnFormatter).Assembly.GetName().Version?.ToString()
            ?? "unknown";

        return Results.Json(new
        {
            ok = true,
            version,
            runtime = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            library = SourcePawnParser.NativeLibraryFileName
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { ok = false, error = ex.Message }, statusCode: 503);
    }
});

app.MapPost("/api/format", (FormatRequest? request) =>
{
    if (request?.Source is null)
        return Results.BadRequest(new { error = "Request body must include a source string." });

    var byteCount = System.Text.Encoding.UTF8.GetByteCount(request.Source);
    if (byteCount > MaxSourceBytes)
    {
        return Results.Json(
            new { error = $"Source exceeds maximum size of {MaxSourceBytes} bytes." },
            statusCode: StatusCodes.Status413PayloadTooLarge);
    }

    var options = request.Options?.ToFormattingOptions() ?? new FormattingOptions
    {
        AllowSyntaxRecovery = false,
        AllowUnsafeMacros = false,
        LineEnding = "\n"
    };

    using var formatter = new SourcePawnFormatter(options);
    var result = formatter.FormatWithResult(request.Source);
    return Results.Json(FormatResponse.FromResult(result));
}).RequireRateLimiting("format");

app.MapPost("/api/modernize", (ModernizeRequest? request) =>
{
    if (request?.Source is null)
        return Results.BadRequest(new { error = "Request body must include a source string." });

    var byteCount = System.Text.Encoding.UTF8.GetByteCount(request.Source);
    if (byteCount > MaxSourceBytes)
    {
        return Results.Json(
            new { error = $"Source exceeds maximum size of {MaxSourceBytes} bytes." },
            statusCode: StatusCodes.Status413PayloadTooLarge);
    }

    var formatting = request.Options?.ToFormattingOptions() ?? new FormattingOptions
    {
        AllowSyntaxRecovery = false,
        AllowUnsafeMacros = false,
        LineEnding = "\n"
    };

    var options = new ModernizeOptions
    {
        FormatAfter = request.FormatAfter,
        FormattingOptions = formatting,
        AllowUnsafeMacros = request.AllowUnsafeMacros || (request.Options?.AllowUnsafeMacros ?? false),
        EnabledRules = request.EnabledRules ?? Array.Empty<string>(),
        ExcludedRules = request.ExcludedRules ?? Array.Empty<string>(),
    };

    using var modernizer = new SourcePawnModernizer(options);
    var result = modernizer.ModernizeWithResult(request.Source);
    return Results.Json(ModernizeResponse.FromResult(result));
}).RequireRateLimiting("format");

app.MapFallbackToFile("index.html");

app.Run();
