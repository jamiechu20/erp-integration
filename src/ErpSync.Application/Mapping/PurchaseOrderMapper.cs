using ErpSync.Application.DTOs;
using ErpSync.Domain.Entities;

namespace ErpSync.Application.Mapping;

public static class PurchaseOrderMapper
{
    public static PurchaseOrder ToEntity(SapPurchaseOrderDto dto, DateTime lastSyncedAt)
    {
        return new PurchaseOrder
        {
            PurchaseOrderNumber = dto.PurchaseOrder,
            PurchaseOrderType = dto.PurchaseOrderType,
            CompanyCode = dto.CompanyCode,
            PurchasingOrganization = dto.PurchasingOrganization,
            PurchasingGroup = dto.PurchasingGroup,
            Supplier = dto.Supplier,
            PurchaseOrderDate = dto.PurchaseOrderDate,
            DocumentCurrency = dto.DocumentCurrency,
            PurchasingCompletenessStatus = dto.PurchasingCompletenessStatus,
            LastSyncedAt = lastSyncedAt,
            Items = dto.Items.Select(ToEntity).ToList(),
        };
    }

    public static PurchaseOrderItem ToEntity(SapPurchaseOrderItemDto dto)
    {
        return new PurchaseOrderItem
        {
            PurchaseOrder = dto.PurchaseOrder,
            PurchaseOrderItemNumber = dto.PurchaseOrderItem,
            Material = dto.Material,
            Plant = dto.Plant,
            OrderQuantity = dto.OrderQuantity,
            PurchaseOrderQuantityUnit = dto.PurchaseOrderQuantityUnit,
            NetPriceAmount = dto.NetPriceAmount,
            NetAmount = dto.NetAmount,
            DeliveryDate = dto.DeliveryDate,
        };
    }
}
