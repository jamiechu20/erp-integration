using System.Text.Json.Serialization;

namespace MockSap.Api.Models;

public class PurchaseOrderHeader
{
    [JsonPropertyName("PurchaseOrder")]
    public string PurchaseOrder { get; set; } = string.Empty;

    [JsonPropertyName("PurchaseOrderType")]
    public string PurchaseOrderType { get; set; } = string.Empty;

    [JsonPropertyName("CompanyCode")]
    public string CompanyCode { get; set; } = string.Empty;

    [JsonPropertyName("PurchasingOrganization")]
    public string PurchasingOrganization { get; set; } = string.Empty;

    [JsonPropertyName("PurchasingGroup")]
    public string PurchasingGroup { get; set; } = string.Empty;

    [JsonPropertyName("Supplier")]
    public string Supplier { get; set; } = string.Empty;

    [JsonPropertyName("PurchaseOrderDate")]
    public DateOnly PurchaseOrderDate { get; set; }

    [JsonPropertyName("DocumentCurrency")]
    public string DocumentCurrency { get; set; } = string.Empty;

    [JsonPropertyName("PurchasingCompletenessStatus")]
    public bool PurchasingCompletenessStatus { get; set; }

    [JsonPropertyName("to_Item")]
    public List<PurchaseOrderItem> Items { get; set; } = new();
}
