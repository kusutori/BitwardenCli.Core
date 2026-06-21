using System.Text.Json.Serialization;
using BitwardenCli.Core.Models;

namespace BitwardenCli.Core.Serialization;

[JsonSerializable(typeof(BitwardenStatus))]
[JsonSerializable(typeof(VaultItem))]
[JsonSerializable(typeof(VaultItem[]))]
[JsonSerializable(typeof(VaultFolder))]
[JsonSerializable(typeof(VaultFolder[]))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class BitwardenJsonContext : JsonSerializerContext;
