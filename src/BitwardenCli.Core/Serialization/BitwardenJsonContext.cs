using System.Text.Json.Serialization;
using BitwardenCli.Core.Models;

namespace BitwardenCli.Core.Serialization;

[JsonSerializable(typeof(BitwardenStatus))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class BitwardenJsonContext : JsonSerializerContext;
