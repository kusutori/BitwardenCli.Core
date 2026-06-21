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

[AttributeUsage(AttributeTargets.Method)]
internal sealed class AuthenticatedCliIntegrationFactAttribute : FactAttribute
{
    public AuthenticatedCliIntegrationFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("BITWARDEN_CLI_RUN_AUTH_TESTS") != "1" ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BITWARDEN_CLI_TEST_EMAIL")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BITWARDEN_CLI_TEST_PASSWORD")))
        {
            Skip = "Set BITWARDEN_CLI_RUN_AUTH_TESTS=1 plus BITWARDEN_CLI_TEST_EMAIL and BITWARDEN_CLI_TEST_PASSWORD to run authenticated isolation tests.";
        }
    }
}
