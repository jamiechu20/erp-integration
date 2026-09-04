namespace ErpSync.Domain.Entities;

public class Anomaly
{
    public int Id { get; set; }
    public string PurchaseOrder { get; set; } = string.Empty;
    public string PurchaseOrderItemNumber { get; set; } = string.Empty;
    public AnomalyRuleType RuleType { get; set; }
    public string Detail { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public DateTime? NotifiedAt { get; set; }
}
