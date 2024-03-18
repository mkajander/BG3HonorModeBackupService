namespace BG3HonorAutoBackupService;

public class BackupService : BackgroundService
{
    private readonly ILogger<BackupService> _logger;
    private FileSystemWatcher? _watcher;
    private readonly string _sourceFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Larian Studios\Baldur's Gate 3\PlayerProfiles\Public\Savegames\Story";    
    private readonly string _targetFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\AutoBackup\BG3\Story";
    private Timer? _debounceTimer;
    private readonly TimeSpan _debouncePeriod = TimeSpan.FromSeconds(5);
    public BackupService(ILogger<BackupService> logger)
    {
        _logger = logger;
    }
    public override Task StartAsync(CancellationToken stoppingToken)
    {
        _watcher = new FileSystemWatcher(_sourceFolder)
        {
            // NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            NotifyFilter = NotifyFilters.LastWrite,
            Filter = "*.*",
            IncludeSubdirectories = true
        };

        _watcher.Changed += OnChanged;
        _watcher.EnableRaisingEvents = true;
        return Task.CompletedTask;
    }
    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.Contains("__HonourMode"))
        {
            _debounceTimer?.Dispose(); // Dispose the existing timer

            // Create a new timer that calls the BackupFolder method after the debounce period
            _debounceTimer = new Timer(_ =>
            {
                BackupFolder(e.FullPath);
                _debounceTimer?.Dispose(); // Clean up the timer
            }, null, _debouncePeriod, Timeout.InfiniteTimeSpan); // Timeout.InfiniteTimeSpan ensures it runs only once
        }
    }
    private void BackupFolder(string fullPath)
    {
        try
        {
            var dirname = Path.GetDirectoryName(fullPath);
            if (dirname == null) return;
            var directory = new DirectoryInfo(dirname);
            var timestampFolder = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var backupDirectory = Path.Combine(_targetFolder, timestampFolder, directory.Name);

            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            foreach (var file in directory.GetFiles())
            {
                var destFile = Path.Combine(backupDirectory, file.Name);
                File.Copy(file.FullName, destFile, true);
            }

            _logger.LogInformation("Backed up savegame to {backupDirectory}", backupDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while backing up savegame");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);
                _logger.LogInformation("Source folder: {SourceFolder}", _sourceFolder);
                _logger.LogInformation("Target folder: {TargetFolder}", _targetFolder);
                _logger.LogInformation("Watcher is enabled: {WatcherEnabled}", _watcher?.EnableRaisingEvents);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
