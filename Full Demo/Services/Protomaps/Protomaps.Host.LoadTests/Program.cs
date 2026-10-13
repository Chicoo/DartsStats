using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

var optionsResult = LoadTestOptions.Parse(args);
if (!optionsResult.IsSuccess)
{
    Console.Error.WriteLine(optionsResult.ErrorMessage);
    Console.Error.WriteLine();
    LoadTestOptions.WriteUsage(Console.Error);
    return 2;
}

var options = optionsResult.Value!;
using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
    Console.WriteLine("Cancellation requested. Waiting for in-flight requests to finish...");
};

using var httpClient = new HttpClient(new SocketsHttpHandler
{
    MaxConnectionsPerServer = options.MaxConcurrency,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
})
{
    BaseAddress = options.BaseUrl,
    Timeout = TimeSpan.FromSeconds(30)
};

var runner = new LoadTestRunner(httpClient, options);
var failed = false;

Console.WriteLine("Protomaps manual load test");
Console.WriteLine(FormattableString.Invariant($"Base URL:        {options.BaseUrl}"));
Console.WriteLine(FormattableString.Invariant($"Path:            {options.Path}"));
Console.WriteLine(FormattableString.Invariant($"Range:           {options.RangeHeader}"));
Console.WriteLine(FormattableString.Invariant($"Rates:           {string.Join(", ", options.Rates)} RPS"));
Console.WriteLine(FormattableString.Invariant($"Duration:        {options.Duration.TotalSeconds:0} seconds per phase"));
Console.WriteLine(FormattableString.Invariant($"Max concurrency: {options.MaxConcurrency}"));
Console.WriteLine(FormattableString.Invariant($"Fail on errors:  {options.FailOnErrors}"));
Console.WriteLine();

foreach (var rate in options.Rates)
{
    if (cancellationTokenSource.IsCancellationRequested)
    {
        failed = true;
        break;
    }

    var summary = await runner.RunPhaseAsync(rate, cancellationTokenSource.Token);
    summary.WriteTo(Console.Out);
    failed |= summary.HasFailures;
}

return options.FailOnErrors && failed ? 1 : 0;

internal sealed class LoadTestRunner(HttpClient httpClient, LoadTestOptions options)
{
    public async Task<PhaseSummary> RunPhaseAsync(int targetRequestsPerSecond, CancellationToken cancellationToken)
    {
        Console.WriteLine(FormattableString.Invariant($"Starting {targetRequestsPerSecond} RPS for {options.Duration.TotalSeconds:0} seconds..."));

        var phaseStopwatch = Stopwatch.StartNew();
        var throttler = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);
        var tasks = new List<Task>();
        var latencies = new ConcurrentBag<long>();
        var statusCodes = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        long attempted = 0;
        long completed = 0;
        long succeeded = 0;
        long failed = 0;
        var interval = TimeSpan.FromMilliseconds(100);
        var intervalTicks = (long)(Stopwatch.Frequency * interval.TotalSeconds);
        var phaseCount = (int)Math.Ceiling(options.Duration.TotalMilliseconds / interval.TotalMilliseconds);
        var scheduledRequests = 0L;

        for (var phaseIndex = 0; phaseIndex < phaseCount; phaseIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var expectedTotal = (long)Math.Round(targetRequestsPerSecond * Math.Min(options.Duration.TotalSeconds, (phaseIndex + 1) * interval.TotalSeconds), MidpointRounding.AwayFromZero);
            var requestsThisInterval = expectedTotal - scheduledRequests;
            scheduledRequests = expectedTotal;

            for (var requestIndex = 0L; requestIndex < requestsThisInterval; requestIndex++)
            {
                await throttler.WaitAsync(cancellationToken);
                Interlocked.Increment(ref attempted);

                tasks.Add(SendRequestAsync(throttler, latencies, statusCodes, () =>
                {
                    Interlocked.Increment(ref completed);
                }, () =>
                {
                    Interlocked.Increment(ref succeeded);
                }, () =>
                {
                    Interlocked.Increment(ref failed);
                }, cancellationToken));
            }

            if (tasks.Count >= options.MaxConcurrency * 2)
            {
                await RemoveCompletedTasksAsync(tasks);
            }

            var nextIntervalTicks = (phaseIndex + 1) * intervalTicks;
            var remainingTicks = nextIntervalTicks - phaseStopwatch.ElapsedTicks;
            if (remainingTicks > 0)
            {
                await DelayForTicksAsync(remainingTicks, cancellationToken);
            }
        }

        await Task.WhenAll(tasks);
        phaseStopwatch.Stop();

        return new PhaseSummary(
            targetRequestsPerSecond,
            phaseStopwatch.Elapsed,
            attempted,
            completed,
            succeeded,
            failed,
            [.. latencies],
            statusCodes.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToDictionary());
    }

    private async Task SendRequestAsync(
        SemaphoreSlim throttler,
        ConcurrentBag<long> latencies,
        ConcurrentDictionary<string, int> statusCodes,
        Action markCompleted,
        Action markSucceeded,
        Action markFailed,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, options.Path);
            request.Headers.Range = options.RangeHeader;

            var requestStopwatch = Stopwatch.StartNew();
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            requestStopwatch.Stop();

            latencies.Add(requestStopwatch.ElapsedMilliseconds);
            statusCodes.AddOrUpdate($"{(int)response.StatusCode} {response.StatusCode}", 1, static (_, count) => count + 1);

            if (response.StatusCode is HttpStatusCode.PartialContent or HttpStatusCode.OK)
            {
                markSucceeded();
            }
            else
            {
                markFailed();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            markFailed();
            statusCodes.AddOrUpdate("Canceled", 1, static (_, count) => count + 1);
        }
        catch (Exception exception)
        {
            markFailed();
            statusCodes.AddOrUpdate(exception.GetType().Name, 1, static (_, count) => count + 1);
        }
        finally
        {
            markCompleted();
            throttler.Release();
        }
    }

    private static async Task DelayForTicksAsync(long ticks, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(ticks / (double)Stopwatch.Frequency);
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }
    }

    private static async Task RemoveCompletedTasksAsync(List<Task> tasks)
    {
        var writeIndex = 0;
        for (var readIndex = 0; readIndex < tasks.Count; readIndex++)
        {
            var task = tasks[readIndex];
            if (task.IsCompleted)
            {
                await task;
                continue;
            }

            tasks[writeIndex++] = task;
        }

        if (writeIndex < tasks.Count)
        {
            tasks.RemoveRange(writeIndex, tasks.Count - writeIndex);
        }
    }
}

internal sealed record PhaseSummary(
    int TargetRequestsPerSecond,
    TimeSpan Elapsed,
    long AttemptedRequests,
    long CompletedRequests,
    long SucceededRequests,
    long FailedRequests,
    long[] LatencyMilliseconds,
    IReadOnlyDictionary<string, int> StatusCounts)
{
    public bool HasFailures => FailedRequests > 0;

    public void WriteTo(TextWriter writer)
    {
        Array.Sort(LatencyMilliseconds);
        var actualRequestsPerSecond = CompletedRequests / Math.Max(Elapsed.TotalSeconds, double.Epsilon);
        var averageLatency = LatencyMilliseconds.Length == 0 ? 0 : LatencyMilliseconds.Average();
        var minimumLatency = LatencyMilliseconds.Length == 0 ? 0 : LatencyMilliseconds[0];
        var maximumLatency = LatencyMilliseconds.Length == 0 ? 0 : LatencyMilliseconds[^1];

        writer.WriteLine(FormattableString.Invariant($"Target RPS:       {TargetRequestsPerSecond}"));
        writer.WriteLine(FormattableString.Invariant($"Attempted:        {AttemptedRequests}"));
        writer.WriteLine(FormattableString.Invariant($"Completed:        {CompletedRequests}"));
        writer.WriteLine(FormattableString.Invariant($"Succeeded:        {SucceededRequests}"));
        writer.WriteLine(FormattableString.Invariant($"Failed:           {FailedRequests}"));
        writer.WriteLine(FormattableString.Invariant($"Actual RPS:       {actualRequestsPerSecond:0.##}"));
        writer.WriteLine(FormattableString.Invariant($"Average latency:  {averageLatency:0.##} ms"));
        writer.WriteLine(FormattableString.Invariant($"p50 latency:      {Percentile(LatencyMilliseconds, 50):0.##} ms"));
        writer.WriteLine(FormattableString.Invariant($"p95 latency:      {Percentile(LatencyMilliseconds, 95):0.##} ms"));
        writer.WriteLine(FormattableString.Invariant($"p99 latency:      {Percentile(LatencyMilliseconds, 99):0.##} ms"));
        writer.WriteLine(FormattableString.Invariant($"Min latency:      {minimumLatency} ms"));
        writer.WriteLine(FormattableString.Invariant($"Max latency:      {maximumLatency} ms"));
        writer.WriteLine("Status counts:");

        if (StatusCounts.Count == 0)
        {
            writer.WriteLine("  <none>");
        }
        else
        {
            foreach (var (status, count) in StatusCounts)
            {
                writer.WriteLine(FormattableString.Invariant($"  {status}: {count}"));
            }
        }

        writer.WriteLine();
    }

    private static double Percentile(long[] sortedValues, int percentile)
    {
        if (sortedValues.Length == 0)
        {
            return 0;
        }

        var position = (sortedValues.Length - 1) * percentile / 100.0;
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);
        if (lowerIndex == upperIndex)
        {
            return sortedValues[lowerIndex];
        }

        var weight = position - lowerIndex;
        return sortedValues[lowerIndex] + ((sortedValues[upperIndex] - sortedValues[lowerIndex]) * weight);
    }
}

internal sealed record LoadTestOptions(
    Uri BaseUrl,
    string Path,
    IReadOnlyList<int> Rates,
    TimeSpan Duration,
    RangeHeaderValue RangeHeader,
    int MaxConcurrency,
    bool FailOnErrors)
{
    private static readonly IReadOnlyDictionary<string, string> Defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["base-url"] = "http://localhost:5138",
        ["path"] = "/protomaps/main.pmtiles",
        ["rates"] = "100,1000,10000",
        ["duration-seconds"] = "60",
        ["range"] = "bytes=0-1023",
        ["max-concurrency"] = "2000",
        ["fail-on-errors"] = "true"
    };

    public static ParseResult<LoadTestOptions> Parse(string[] args)
    {
        var values = new Dictionary<string, string>(Defaults, StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument is "-h" or "--help")
            {
                WriteUsage(Console.Out);
                Environment.Exit(0);
            }

            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                return ParseResult<LoadTestOptions>.Failure($"Unexpected argument '{argument}'. Options must start with '--'.");
            }

            var optionName = argument[2..];
            if (!Defaults.ContainsKey(optionName))
            {
                return ParseResult<LoadTestOptions>.Failure($"Unknown option '--{optionName}'.");
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                return ParseResult<LoadTestOptions>.Failure($"Option '--{optionName}' requires a value.");
            }

            values[optionName] = args[++index];
        }

        if (!Uri.TryCreate(values["base-url"], UriKind.Absolute, out var baseUrl)
            || baseUrl.Scheme is not ("http" or "https"))
        {
            return ParseResult<LoadTestOptions>.Failure("Option '--base-url' must be an absolute HTTP or HTTPS URL.");
        }

        var path = values["path"];
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
        {
            return ParseResult<LoadTestOptions>.Failure("Option '--path' must start with '/'.");
        }

        if (!TryParseRates(values["rates"], out var rates, out var ratesError))
        {
            return ParseResult<LoadTestOptions>.Failure(ratesError);
        }

        if (!int.TryParse(values["duration-seconds"], NumberStyles.None, CultureInfo.InvariantCulture, out var durationSeconds)
            || durationSeconds <= 0)
        {
            return ParseResult<LoadTestOptions>.Failure("Option '--duration-seconds' must be a positive whole number.");
        }

        if (!RangeHeaderValue.TryParse(values["range"], out var rangeHeader))
        {
            return ParseResult<LoadTestOptions>.Failure("Option '--range' must be a valid HTTP Range header value such as 'bytes=0-1023'.");
        }

        if (!int.TryParse(values["max-concurrency"], NumberStyles.None, CultureInfo.InvariantCulture, out var maxConcurrency)
            || maxConcurrency <= 0)
        {
            return ParseResult<LoadTestOptions>.Failure("Option '--max-concurrency' must be a positive whole number.");
        }

        if (!bool.TryParse(values["fail-on-errors"], out var failOnErrors))
        {
            return ParseResult<LoadTestOptions>.Failure("Option '--fail-on-errors' must be 'true' or 'false'.");
        }

        return ParseResult<LoadTestOptions>.Success(new LoadTestOptions(
            baseUrl,
            path,
            rates,
            TimeSpan.FromSeconds(durationSeconds),
            rangeHeader,
            maxConcurrency,
            failOnErrors));
    }

    public static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("Usage:");
        writer.WriteLine("  dotnet run --project webservices/Protomaps/Protomaps.Host.LoadTests/Protomaps.Host.LoadTests.csproj -- [options]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --base-url <url>             Base service URL. Default: http://localhost:5138");
        writer.WriteLine("  --path <path>                Request path. Default: /protomaps/main.pmtiles");
        writer.WriteLine("  --rates <csv>                Target RPS values. Default: 100,1000,10000");
        writer.WriteLine("  --duration-seconds <number>  Seconds per phase. Default: 60");
        writer.WriteLine("  --range <range>              HTTP Range header. Default: bytes=0-1023");
        writer.WriteLine("  --max-concurrency <number>   Maximum in-flight requests. Default: 2000");
        writer.WriteLine("  --fail-on-errors <bool>      Return exit code 1 when failures occur. Default: true");
    }

    private static bool TryParseRates(string value, out IReadOnlyList<int> rates, out string errorMessage)
    {
        var parsedRates = new List<int>();
        foreach (var part in value.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var rate) || rate <= 0)
            {
                rates = [];
                errorMessage = "Option '--rates' must contain positive whole numbers separated by commas.";
                return false;
            }

            parsedRates.Add(rate);
        }

        if (parsedRates.Count == 0)
        {
            rates = [];
            errorMessage = "Option '--rates' must contain at least one positive whole number.";
            return false;
        }

        rates = parsedRates;
        errorMessage = string.Empty;
        return true;
    }
}

internal sealed record ParseResult<T>(bool IsSuccess, T? Value, string ErrorMessage)
{
    public static ParseResult<T> Success(T value) => new(true, value, string.Empty);

    public static ParseResult<T> Failure(string errorMessage) => new(false, default, errorMessage);
}
