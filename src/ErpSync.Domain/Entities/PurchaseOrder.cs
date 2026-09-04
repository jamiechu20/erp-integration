namespace ErpSync.Domain.Entities;

public class PurchaseOrder
{
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string PurchaseOrderType { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public string PurchasingOrganization { get; set; } = string.Empty;
    public string PurchasingGroup { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public DateOnly PurchaseOrderDate { get; set; }
    public string DocumentCurrency { get; set; } = string.Empty;
    public bool PurchasingCompletenessStatus { get; set; }
    public DateTime LastSyncedAt { get; set; }

    public List<PurchaseOrderItem> Items { get; set; } = new();
}
