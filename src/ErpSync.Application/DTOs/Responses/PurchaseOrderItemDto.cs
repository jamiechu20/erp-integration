namespace ErpSync.Application.DTOs.Responses;

public record PurchaseOrderItemDto(
    string PurchaseOrder,
    string PurchaseOrderItemNumber,
    string Material,
    string Plant,
    decimal OrderQuantity,
    string PurchaseOrderQuantityUnit,
    decimal NetPriceAmount,
    decimal NetAmount,
    DateOnly DeliveryDate);
