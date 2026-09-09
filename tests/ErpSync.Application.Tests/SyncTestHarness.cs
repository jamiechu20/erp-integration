using ErpSync.Application.DTOs;
using ErpSync.Application.Services;
using ErpSync.Application.Tests.Fakes;
using ErpSync.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace ErpSync.Application.Tests;

/// <summary>
/// 把 PurchaseOrderSyncService 與它的六個 Fake 相依綁在一起，測試只要
/// 「安排 Mock API 要回什麼 → 跑同步 → 讀 Fake 的內容斷言」。
/// </summary>
public class SyncTestHarness
{
    public FakeSapPurchaseOrderClient SapClient { get; } = new();
    public FakePurchaseOrderRepository PurchaseOrders { get; } = new();
    public FakeAnomalyRepository Anomalies { get; } = new();
    public FakeSyncLogRepository SyncLogs { get; } = new();
    public FakeUnitOfWork UnitOfWork { get; } = new();
    public FakeAnomalyNotifier Notifier { get; } = new();

    public PurchaseOrderSyncService Service => new(
        SapClient, PurchaseOrders, Anomalies, SyncLogs, UnitOfWork, Notifier,
        NullLogger<PurchaseOrderSyncService>.Instance);

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
    public static DateOnly DaysFromToday(int days) => Today.AddDays(days);

    /// <summary>設定 Mock API 這次同步會回傳的採購單。</summary>
    public SyncTestHarness WithSapOrders(params SapPurchaseOrderDto[] orders)
    {
        SapClient.Orders = orders.ToList();
        return this;
    }

    /// <summary>
    /// 在「本地 DB」預先放一筆已同步過的品項，用來提供價格規則的歷史均價基準，
    /// 或測試重複同步不重複寫入。
    /// </summary>
    public SyncTestHarness WithExistingItem(
        string purchaseOrder, string itemNumber, string material, decimal netPrice, DateOnly? deliveryDate = null)
    {
        var header = PurchaseOrders.Orders.FirstOrDefault(o => o.PurchaseOrderNumber == purchaseOrder);
        if (header is null)
        {
            header = new PurchaseOrder
            {
                PurchaseOrderNumber = purchaseOrder,
                CompanyCode = "1710",
                Supplier = "17300001",
                LastSyncedAt = DateTime.UtcNow,
            };
            PurchaseOrders.Orders.Add(header);
        }

        header.Items.Add(new PurchaseOrderItem
        {
            PurchaseOrder = purchaseOrder,
            PurchaseOrderItemNumber = itemNumber,
            Material = material,
            NetPriceAmount = netPrice,
            DeliveryDate = deliveryDate ?? DaysFromToday(7),
        });
        return this;
    }

    public static SapPurchaseOrderDto SapOrder(
        string purchaseOrder, bool completenessStatus, params SapPurchaseOrderItemDto[] items)
        => new()
        {
            PurchaseOrder = purchaseOrder,
            PurchaseOrderType = "NB",
            CompanyCode = "1710",
            PurchasingOrganization = "1710",
            PurchasingGroup = "001",
            Supplier = "17300001",
            PurchaseOrderDate = DaysFromToday(-30),
            DocumentCurrency = "USD",
            PurchasingCompletenessStatus = completenessStatus,
            Items = items.ToList(),
        };

    public static SapPurchaseOrderItemDto SapItem(
        string purchaseOrder, string itemNumber, string material, decimal netPrice, DateOnly deliveryDate)
        => new()
        {
            PurchaseOrder = purchaseOrder,
            PurchaseOrderItem = itemNumber,
            Material = material,
            Plant = "1710",
            OrderQuantity = 10,
            PurchaseOrderQuantityUnit = "EA",
            NetPriceAmount = netPrice,
            NetAmount = netPrice * 10,
            DeliveryDate = deliveryDate,
        };
}
