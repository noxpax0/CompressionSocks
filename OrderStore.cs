using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CompressionGarmentOrder;

internal sealed class OrderStore
{
    private const string Magic = "MERCURY2";
    private const int MaximumOrders = 500;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Mercury.OrderStore.v2");
    private readonly string _path;
    private readonly object _gate = new();

    public OrderStore(string? dataDirectory = null)
    {
        string root = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mercury");
        _path = Path.Combine(root, "orders.dat");
    }

    public string? GetDraftJson()
    {
        lock (_gate) return Load().Draft?.ToJsonString();
    }

    public string GetOrdersJson()
    {
        lock (_gate) return Load().Orders.ToJsonString();
    }

    public void SaveDraft(string orderJson)
    {
        JsonNode payload = ParseOrder(orderJson);
        lock (_gate)
        {
            Database database = Load();
            payload["savedAt"] = DateTimeOffset.UtcNow.ToString("O");
            database.Draft = payload;
            Save(database);
        }
    }

    public void DiscardDraft()
    {
        lock (_gate)
        {
            Database database = Load();
            if (database.Draft is null) return;
            database.Draft = null;
            Save(database);
        }
    }

    public string CompleteOrder(string orderJson)
    {
        JsonNode payload = ParseOrder(orderJson);
        lock (_gate)
        {
            Database database = Load();
            string? requestedId = payload["orderId"]?.GetValue<string>();
            string id = requestedId is { Length: 32 } && requestedId.All(Uri.IsHexDigit)
                ? requestedId
                : Guid.NewGuid().ToString("N");
            string timestamp = DateTimeOffset.UtcNow.ToString("O");
            database.Orders.Add(new JsonObject
            {
                ["id"] = id,
                ["status"] = "completed",
                ["createdAt"] = timestamp,
                ["savedAt"] = timestamp,
                ["data"] = payload
            });
            while (database.Orders.Count > MaximumOrders) database.Orders.RemoveAt(0);
            database.Draft = null;
            Save(database);
            return id;
        }
    }

    public void SetOrderStatus(string id, string status)
    {
        if (id.Length != 32 || !id.All(Uri.IsHexDigit) || status is not ("completed" or "archived"))
            throw new InvalidDataException("The order status request is invalid.");
        lock (_gate)
        {
            Database database = Load();
            JsonObject? order = database.Orders.OfType<JsonObject>()
                .FirstOrDefault(item => string.Equals(item["id"]?.GetValue<string>(), id, StringComparison.Ordinal));
            if (order is null) return;
            order["status"] = status;
            order["statusChangedAt"] = DateTimeOffset.UtcNow.ToString("O");
            Save(database);
        }
    }

    private Database Load()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        if (!File.Exists(_path)) return MigrateLegacy();
        try
        {
            return ReadDatabase(_path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or JsonException or InvalidDataException)
        {
            string backup = _path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            try { File.Copy(_path, backup, false); } catch { }
            try { return ReadDatabase(_path + ".bak"); }
            catch { return new Database(null, [], 2); }
        }
    }

    private static Database ReadDatabase(string path)
    {
        byte[] envelope = File.ReadAllBytes(path);
        if (envelope.Length <= Magic.Length ||
            !envelope.AsSpan(0, Magic.Length).SequenceEqual(Encoding.ASCII.GetBytes(Magic)))
            throw new InvalidDataException("The order database header is invalid.");
        byte[] clear = ProtectedData.Unprotect(envelope[Magic.Length..], Entropy, DataProtectionScope.CurrentUser);
        JsonObject root = JsonNode.Parse(clear) as JsonObject
            ?? throw new InvalidDataException("The order database is invalid.");
        return new Database(root["draft"], root["orders"] as JsonArray ?? [], root["schemaVersion"]?.GetValue<int>() ?? 2);
    }

    private Database MigrateLegacy()
    {
        string legacyPath = Path.Combine(Path.GetDirectoryName(_path)!, "orders.json");
        if (!File.Exists(legacyPath)) return new Database(null, [], 2);
        try
        {
            JsonObject legacy = JsonNode.Parse(File.ReadAllText(legacyPath)) as JsonObject ?? [];
            JsonArray legacyOrders = legacy["orders"] as JsonArray ?? [];
            JsonNode? draft = legacyOrders.LastOrDefault()?["data"]?.DeepClone();
            var migratedOrders = new JsonArray();
            foreach (JsonNode? item in legacyOrders)
            {
                if (item is not JsonObject source || source["data"] is null) continue;
                migratedOrders.Add(new JsonObject
                {
                    ["id"] = source["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N"),
                    ["status"] = "migrated",
                    ["createdAt"] = source["savedAt"]?.GetValue<string>() ?? DateTimeOffset.UtcNow.ToString("O"),
                    ["savedAt"] = source["savedAt"]?.GetValue<string>() ?? DateTimeOffset.UtcNow.ToString("O"),
                    ["data"] = source["data"]!.DeepClone()
                });
            }
            var migrated = new Database(draft, migratedOrders, 2);
            Save(migrated);
            File.Move(legacyPath, legacyPath + ".migrated", true);
            return migrated;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new Database(null, [], 2);
        }
    }

    private void Save(Database database)
    {
        var root = new JsonObject
        {
            ["schemaVersion"] = database.SchemaVersion,
            ["savedAt"] = DateTimeOffset.UtcNow.ToString("O"),
            ["draft"] = database.Draft?.DeepClone(),
            ["orders"] = database.Orders.DeepClone()
        };
        byte[] clear = JsonSerializer.SerializeToUtf8Bytes(root);
        byte[] encrypted = ProtectedData.Protect(clear, Entropy, DataProtectionScope.CurrentUser);
        byte[] envelope = new byte[Magic.Length + encrypted.Length];
        Encoding.ASCII.GetBytes(Magic).CopyTo(envelope, 0);
        encrypted.CopyTo(envelope, Magic.Length);

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        string temporary = _path + ".tmp";
        string backup = _path + ".bak";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            stream.Write(envelope);
            stream.Flush(true);
        }
        if (File.Exists(_path)) File.Replace(temporary, _path, backup, true);
        else File.Move(temporary, _path);
    }

    private static JsonNode ParseOrder(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > 256_000) throw new InvalidDataException("The order is too large.");
        return JsonNode.Parse(json) ?? throw new InvalidDataException("The order is invalid.");
    }

    private sealed class Database(JsonNode? draft, JsonArray orders, int schemaVersion)
    {
        public JsonNode? Draft { get; set; } = draft;
        public JsonArray Orders { get; } = orders;
        public int SchemaVersion { get; } = schemaVersion;
    }
}
