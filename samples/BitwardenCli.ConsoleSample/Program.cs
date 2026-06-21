using BitwardenCli.Core;
using BitwardenCli.Core.Accounts;

var dataDirectory = Path.Combine(Path.GetTempPath(), "BitwardenCli.Core.Sample", "cli");
var profile = new BitwardenAccountProfile
{
    Id = Guid.Parse("d0644a8d-275c-48ce-b167-7bd343aef521"),
    DisplayName = "Console sample",
    CliDataDirectory = dataDirectory
};

var client = new BitwardenCliClientFactory(new BitwardenCliOptions
{
    ExecutablePath = Environment.GetEnvironmentVariable("BW_PATH") ?? "bw"
}).Create(profile);

var status = await client.GetStatusAsync();
Console.WriteLine(status.IsSuccess
    ? $"Status: {status.Value?.Status}"
    : $"Status error: {status.Error?.Code} - {status.Error?.Message}");
