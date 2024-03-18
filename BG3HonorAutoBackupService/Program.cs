using BG3HonorAutoBackupService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "BG3HonorAutoBackupService";
});

builder.Services.AddHostedService<BackupService>();

var host = builder.Build();
host.Run();