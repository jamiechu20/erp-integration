using ErpSync.Application.DTOs;
using ErpSync.Application.Interfaces;

namespace ErpSync.Application.Tests.Fakes;

/// <summary>回傳測試指定的採購單，取代真正打 Mock SAP API 的 HttpClient。</summary>
public class FakeSapPurchaseOrderClient : ISapPurchaseOrderClient
{
    public List<SapPurchaseOrderDto> Orders { get; set; } = new();

    public Task<List<SapPurchaseOrderDto>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Orders);
}
