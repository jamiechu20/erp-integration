using ErpSync.Application.DTOs.Responses;
using ErpSync.Domain.Entities;

namespace ErpSync.Application.Mapping;

/// <summary>
/// Entity → API 回應 DTO 的映射（spec.md §10 查詢端點），與 PurchaseOrderMapper（Sap DTO →
/// Entity，同步用）方向相反，分開放避免混淆。
/// </summary>
public static class ResponseMapper
{
    public static PurchaseOrderDto ToDto(PurchaseOrder entity)
    {
        return new PurchaseOrderDto(
            entity.PurchaseOrderNumber,
            entity.PurchaseOrderType,
            entity.CompanyCode,
            entity.PurchasingOrganization,
            entity.PurchasingGroup,
            entity.Supplier,
            entity.PurchaseOrderDate,
            entity.DocumentCurrency,
            entity.PurchasingCompletenessStatus,
            entity.LastSyncedAt);
    }

    public static PurchaseOrderItemDto ToDto(PurchaseOrderItem entity)
    {
        return new PurchaseOrderItemDto(
            entity.PurchaseOrder,
            entity.PurchaseOrderItemNumber,
            entity.Material,
            entity.Plant,
            entity.OrderQuantity,
            entity.PurchaseOrderQuantityUnit,
            entity.NetPriceAmount,
            entity.NetAmount,
            entity.DeliveryDate);
    }

    public static AnomalyDto ToDto(Anomaly entity)
    {
        return new AnomalyDto(
            entity.Id,
            entity.PurchaseOrder,
            entity.PurchaseOrderItemNumber,
            entity.RuleType.ToString(),
            entity.Detail,
            entity.DetectedAt,
            entity.NotifiedAt);
    }

    public static SyncLogDto ToDto(SyncLog entity)
    {
        return new SyncLogDto(
            entity.Id,
            entity.RunAt,
            entity.RecordsFetched,
            entity.RecordsNew,
            entity.AnomaliesFound);
    }
}
