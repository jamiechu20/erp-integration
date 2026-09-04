namespace ErpSync.Domain.Entities;

public class PurchaseOrderItem
{
    public string PurchaseOrder { get; set; } = string.Empty;
    public string PurchaseOrderItemNumber { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public decimal OrderQuantity { get; set; }
    public string PurchaseOrderQuantityUnit { get; set; } = string.Empty;
    public decimal NetPriceAmount { get; set; }
    public decimal NetAmount { get; set; }
    public DateOnly DeliveryDate { get; set; }
}
