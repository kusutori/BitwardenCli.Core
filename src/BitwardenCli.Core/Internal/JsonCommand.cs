using System.Text;
using System.Text.Json.Nodes;
using BitwardenCli.Core.Execution;

namespace BitwardenCli.Core.Internal;

internal static class JsonCommand
{
    public static CliCommand WithPayload(string operation, JsonNode payload, params CliArgument[] arguments) =>
        new(operation, arguments)
        {
            StandardInput = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload.ToJsonString())),
            IsStandardInputSensitive = true
        };
}
