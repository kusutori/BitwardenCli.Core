namespace BitwardenCli.Core.Tests.TestSupport;

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"BitwardenCli.Core.Tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string GetPath(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        var tempRoot = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath());
        var target = System.IO.Path.GetFullPath(Path);
        if (target.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(target))
        {
            Directory.Delete(target, recursive: true);
        }
    }
}
