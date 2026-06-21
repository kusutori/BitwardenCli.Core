using System.Text.Json.Serialization;
using BitwardenCli.Core.Models;

namespace BitwardenCli.Core.Serialization;

[JsonSerializable(typeof(BitwardenStatus))]
[JsonSerializable(typeof(VaultItem))]
[JsonSerializable(typeof(VaultItem[]))]
[JsonSerializable(typeof(VaultFolder))]
[JsonSerializable(typeof(VaultFolder[]))]
[JsonSerializable(typeof(BitwardenOrganization))]
[JsonSerializable(typeof(BitwardenOrganization[]))]
[JsonSerializable(typeof(BitwardenCollection))]
[JsonSerializable(typeof(BitwardenCollection[]))]
[JsonSerializable(typeof(BitwardenSend))]
[JsonSerializable(typeof(BitwardenSend[]))]
[JsonSerializable(typeof(OrganizationMember[]))]
[JsonSerializable(typeof(DeviceApprovalRequest[]))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class BitwardenJsonContext : JsonSerializerContext;
