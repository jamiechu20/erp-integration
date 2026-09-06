using ErpSync.Application.DTOs.Responses;

namespace ErpSync.Application.Interfaces;

public interface IPurchaseOrderQueryService
{
    Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync(
        string? supplier, string? companyCode, CancellationToken cancellationToken = default);

    /// <summary>找不到採購單時回傳 null，由 Controller 轉成 404。</summary>
    Task<List<PurchaseOrderItemDto>?> GetItemsAsync(
        string purchaseOrderId, CancellationToken cancellationToken = default);
}
