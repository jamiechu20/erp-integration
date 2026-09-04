using System.Text.Json.Serialization;

namespace MockSap.Api.Models;

public class PurchaseOrderItem
{
    [JsonPropertyName("PurchaseOrder")]
    public string PurchaseOrder { get; set; } = string.Empty;

    [JsonPropertyName("PurchaseOrderItem")]
    public string PurchaseOrderItemNumber { get; set; } = string.Empty;

    [JsonPropertyName("Material")]
    public string Material { get; set; } = string.Empty;

    [JsonPropertyName("Plant")]
    public string Plant { get; set; } = string.Empty;

    [JsonPropertyName("OrderQuantity")]
    public decimal OrderQuantity { get; set; }

    [JsonPropertyName("PurchaseOrderQuantityUnit")]
    public string PurchaseOrderQuantityUnit { get; set; } = string.Empty;

    [JsonPropertyName("NetPriceAmount")]
    public decimal NetPriceAmount { get; set; }

    [JsonPropertyName("NetAmount")]
    public decimal NetAmount { get; set; }

    [JsonPropertyName("DeliveryDate")]
    public DateOnly DeliveryDate { get; set; }
}
