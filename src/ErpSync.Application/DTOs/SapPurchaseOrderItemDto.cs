using System.Text.Json.Serialization;

namespace ErpSync.Application.DTOs;

/// <summary>
/// 對應 SAP API_PURCHASEORDER_PROCESS_SRV 的 PurchaseOrderItem entity（Mock API 同步比照）。
/// </summary>
public class SapPurchaseOrderItemDto
{
    [JsonPropertyName("PurchaseOrder")]
    public string PurchaseOrder { get; set; } = string.Empty;

    [JsonPropertyName("PurchaseOrderItem")]
    public string PurchaseOrderItem { get; set; } = string.Empty;

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
