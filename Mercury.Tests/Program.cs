using System.Text.Json;
using CompressionGarmentOrder;

string root = Path.Combine(Path.GetTempPath(), "Mercury.Tests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var store = new OrderStore(root);
    const string patientName = "TEST-PATIENT-NAME";
    string order = JsonSerializer.Serialize(new
    {
        version = 1,
        patient = new { name = patientName, contact = "+356 12345", location = "12 Test Street", pairs = "1", gender = "female" },
        compressionId = "light",
        garmentId = "knee",
        garmentOption = "Open toe",
        measurements = new { calf = "34", ankle = "22" },
        colourPriority = Array.Empty<string>(),
        notes = ""
    });

    store.SaveDraft(order);
    Assert(store.GetDraftJson()?.Contains(patientName, StringComparison.Ordinal) == true, "Draft round-trip failed.");
    byte[] encrypted = File.ReadAllBytes(Path.Combine(root, "orders.dat"));
    Assert(!System.Text.Encoding.UTF8.GetString(encrypted).Contains(patientName, StringComparison.Ordinal), "Patient data was stored in plaintext.");

    string id = store.CompleteOrder(order);
    Assert(id.Length == 32, "Completed order ID is invalid.");
    Assert(store.GetDraftJson() is null, "Completing an order did not remove the draft.");
    using JsonDocument orders = JsonDocument.Parse(store.GetOrdersJson());
    Assert(orders.RootElement.GetArrayLength() == 1, "Completed order was not added to history.");
    Assert(orders.RootElement[0].GetProperty("status").GetString() == "completed", "Completed order status is invalid.");
    store.SetOrderStatus(id, "archived");
    using JsonDocument archivedOrders = JsonDocument.Parse(store.GetOrdersJson());
    Assert(archivedOrders.RootElement[0].GetProperty("status").GetString() == "archived", "Archiving an order failed.");
    store.SetOrderStatus(id, "completed");

    store.SaveDraft(order);
    store.DiscardDraft();
    Assert(store.GetDraftJson() is null, "Discarding a draft failed.");

    Console.WriteLine("Mercury tests passed.");
    return 0;
}
finally
{
    try { Directory.Delete(root, true); } catch { }
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
