namespace ErpSync.Application.DTOs.Responses;

public record PurchaseOrderDto(
    string PurchaseOrderNumber,
    string PurchaseOrderType,
    string CompanyCode,
    string PurchasingOrganization,
    string PurchasingGroup,
    string Supplier,
    DateOnly PurchaseOrderDate,
    string DocumentCurrency,
    bool PurchasingCompletenessStatus,
    DateTime LastSyncedAt);
