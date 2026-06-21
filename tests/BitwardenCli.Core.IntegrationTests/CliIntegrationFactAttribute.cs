namespace BitwardenCli.Core.IntegrationTests;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class CliIntegrationFactAttribute : FactAttribute
{
    public CliIntegrationFactAttribute()
    {
        if (!string.Equals(
            Environment.GetEnvironmentVariable("BITWARDEN_CLI_RUN_INTEGRATION_TESTS"),
            "1",
            StringComparison.Ordinal))
        {
            Skip = "Set BITWARDEN_CLI_RUN_INTEGRATION_TESTS=1 to run tests against the installed bw executable.";
        }
    }
}
