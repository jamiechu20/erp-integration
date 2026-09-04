using ErpSync.Application.DTOs;

namespace ErpSync.Application.Interfaces;

public interface ISapPurchaseOrderClient
{
    Task<List<SapPurchaseOrderDto>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default);
}
