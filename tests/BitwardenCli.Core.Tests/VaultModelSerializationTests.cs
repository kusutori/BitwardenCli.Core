using System.Text.Json;
using BitwardenCli.Core.Models;

namespace BitwardenCli.Core.Tests;

public sealed class VaultModelSerializationTests
{
    [Fact]
    public void Parses_nullable_dates_attachments_and_unknown_fields()
    {
        const string json = """
            {
              "object": "item",
              "id": "item-id",
              "type": 1,
              "name": "Example",
              "revisionDate": "2026-06-21T01:02:03.456Z",
              "deletedDate": null,
              "collectionIds": [],
              "login": { "uris": [{ "match": null, "uri": "https://example.com" }], "username": "user" },
              "attachments": [{ "id": "attachment-id", "fileName": "note.txt", "size": 12, "sizeName": "12 bytes" }],
              "futureSchemaField": { "version": 2 }
            }
            """;

        var item = JsonSerializer.Deserialize<VaultItem>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(item);
        Assert.Equal(2026, item.RevisionDate?.Year);
        Assert.Null(item.DeletedDate);
        Assert.Equal("note.txt", Assert.Single(item.Attachments).FileName);
        Assert.Equal(2, item.AdditionalProperties!["futureSchemaField"].GetProperty("version").GetInt32());
    }
}
