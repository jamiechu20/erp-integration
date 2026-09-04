using System.Net.Http.Json;
using ErpSync.Application.DTOs;
using ErpSync.Application.Interfaces;

namespace ErpSync.Infrastructure.Sap;

/// <summary>
/// 呼叫 SAP API_PURCHASEORDER_PROCESS_SRV 相容端點（目前指向 MockSap.Api）。
/// 正式環境只需替換 HttpClient 的 BaseAddress。
/// </summary>
public class SapPurchaseOrderClient : ISapPurchaseOrderClient
{
    private readonly HttpClient _httpClient;

    public SapPurchaseOrderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<SapPurchaseOrderDto>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<SapODataResponseDto<SapPurchaseOrderDto>>(
            "/PurchaseOrder?$expand=to_Item", cancellationToken);

        return response?.Value ?? new List<SapPurchaseOrderDto>();
    }
}
