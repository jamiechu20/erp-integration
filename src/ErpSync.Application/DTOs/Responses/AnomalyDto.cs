namespace ErpSync.Application.DTOs.Responses;

public record AnomalyDto(
    int Id,
    string PurchaseOrder,
    string PurchaseOrderItemNumber,
    string RuleType,
    string Detail,
    DateTime DetectedAt,
    DateTime? NotifiedAt);
