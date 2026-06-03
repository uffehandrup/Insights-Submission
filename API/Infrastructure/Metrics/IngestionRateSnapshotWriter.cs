using System.Text;

namespace API.Infrastructure.Metrics;

/// <summary>
/// Periodically samples <see cref="IngestionRateTracker"/> and appends one CSV row per tick
/// (timestamp, totalEvents, windowEvents, eventsPerSecond) so ingestion throughput can be
/// reviewed in Excel / pandas after a load test run.
/// </summary>
public sealed class IngestionRateSnapshotWriter : BackgroundService
{
    private const string CsvHeader = "timestamp,totalEvents,windowEvents,eventsPerSecond";

    private readonly IngestionRateTracker _tracker;
    private readonly ILogger<IngestionRateSnapshotWriter> _logger;
    private readonly string _filePath;
    private readonly TimeSpan _interval;

    public IngestionRateSnapshotWriter(
        IngestionRateTracker tracker,
        IConfiguration configuration,
        ILogger<IngestionRateSnapshotWriter> logger)
    {
        _tracker = tracker;
        _logger = logger;

        var relativePath = configuration["Metrics:SnapshotFilePath"] ?? "data/ingestion-rate.csv";
        _filePath = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(AppContext.BaseDirectory, relativePath);

        var seconds = configuration.GetValue<int?>("Metrics:SnapshotIntervalSeconds") ?? 5;
        _interval = TimeSpan.FromSeconds(Math.Max(1, seconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        if (!File.Exists(_filePath))
            await File.WriteAllTextAsync(_filePath, CsvHeader + Environment.NewLine, stoppingToken);

        _logger.LogInformation("Ingestion rate snapshots → {Path} every {Interval}", _filePath, _interval);

        var previousTotal = _tracker.TotalEvents;
        var previousTime = DateTime.UtcNow;

        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var now = DateTime.UtcNow;
                var total = _tracker.TotalEvents;
                var windowEvents = total - previousTotal;
                var elapsedSeconds = (now - previousTime).TotalSeconds;
                var rate = elapsedSeconds > 0 ? windowEvents / elapsedSeconds : 0d;

                var row = FormattableString.Invariant(
                    $"{now:O},{total},{windowEvents},{rate:F2}{Environment.NewLine}");

                await File.AppendAllTextAsync(_filePath, row, Encoding.UTF8, stoppingToken);

                previousTotal = total;
                previousTime = now;
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }
    }
}
