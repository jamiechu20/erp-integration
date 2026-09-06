using ErpSync.Application.DTOs.Responses;
using ErpSync.Application.Interfaces;
using ErpSync.Application.Mapping;

namespace ErpSync.Application.Services;

public class PurchaseOrderQueryService : IPurchaseOrderQueryService
{
    private readonly IPurchaseOrderRepository _repository;

    public PurchaseOrderQueryService(IPurchaseOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PurchaseOrderDto>> GetPurchaseOrdersAsync(
        string? supplier, string? companyCode, CancellationToken cancellationToken = default)
    {
        var orders = await _repository.GetAllAsync(supplier, companyCode, cancellationToken);
        return orders.Select(ResponseMapper.ToDto).ToList();
    }

    public async Task<List<PurchaseOrderItemDto>?> GetItemsAsync(
        string purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdWithItemsAsync(purchaseOrderId, cancellationToken);
        return order?.Items.Select(ResponseMapper.ToDto).ToList();
    }
}
