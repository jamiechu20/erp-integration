namespace MockSap.Api.Models;

/// <summary>
/// 記憶體儲存，模擬 SAP 端資料。種子資料的日期以「啟動當下」為基準動態產生，
/// 確保不論哪一天啟動 demo，逾期異常都還是逾期。
/// </summary>
public class PurchaseOrderStore
{
    private readonly List<PurchaseOrderHeader> _orders = new();
    private readonly object _lock = new();

    public PurchaseOrderStore()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _orders.AddRange(BuildSeedData(today));
    }

    public IReadOnlyList<PurchaseOrderHeader> GetAll()
    {
        lock (_lock)
        {
            return _orders.ToList();
        }
    }

    public void Add(PurchaseOrderHeader order)
    {
        lock (_lock)
        {
            _orders.Add(order);
        }
    }

    private static List<PurchaseOrderHeader> BuildSeedData(DateOnly today)
    {
        return new List<PurchaseOrderHeader>
        {
            // 逾期異常：交期已過，且尚未完成收貨
            new()
            {
                PurchaseOrder = "4500000010",
                PurchaseOrderType = "NB",
                CompanyCode = "1000",
                PurchasingOrganization = "US01",
                PurchasingGroup = "A10",
                Supplier = "2000000010",
                PurchaseOrderDate = today.AddDays(-30),
                DocumentCurrency = "USD",
                PurchasingCompletenessStatus = false,
                Items = new List<PurchaseOrderItem>
                {
                    new()
                    {
                        PurchaseOrder = "4500000010",
                        PurchaseOrderItemNumber = "00010",
                        Material = "MAT-1001",
                        Plant = "PL01",
                        OrderQuantity = 100,
                        PurchaseOrderQuantityUnit = "EA",
                        NetPriceAmount = 25.5m,
                        NetAmount = 2550m,
                        DeliveryDate = today.AddDays(-14),
                    },
                },
            },
            // 正常：同物料，單價落在正常區間，作為價格異常比對基準
            new()
            {
                PurchaseOrder = "4500000011",
                PurchaseOrderType = "NB",
                CompanyCode = "1000",
                PurchasingOrganization = "US01",
                PurchasingGroup = "A10",
                Supplier = "2000000011",
                PurchaseOrderDate = today.AddDays(-10),
                DocumentCurrency = "USD",
                PurchasingCompletenessStatus = true,
                Items = new List<PurchaseOrderItem>
                {
                    new()
                    {
                        PurchaseOrder = "4500000011",
                        PurchaseOrderItemNumber = "00010",
                        Material = "MAT-1001",
                        Plant = "PL01",
                        OrderQuantity = 50,
                        PurchaseOrderQuantityUnit = "EA",
                        NetPriceAmount = 26.0m,
                        NetAmount = 1300m,
                        DeliveryDate = today.AddDays(20),
                    },
                },
            },
            // 價格異常：同物料歷史均價 ~25.75，此筆單價 60，偏差超過 30%
            new()
            {
                PurchaseOrder = "4500000012",
                PurchaseOrderType = "NB",
                CompanyCode = "1000",
                PurchasingOrganization = "US01",
                PurchasingGroup = "A10",
                Supplier = "2000000012",
                PurchaseOrderDate = today.AddDays(-5),
                DocumentCurrency = "USD",
                PurchasingCompletenessStatus = true,
                Items = new List<PurchaseOrderItem>
                {
                    new()
                    {
                        PurchaseOrder = "4500000012",
                        PurchaseOrderItemNumber = "00010",
                        Material = "MAT-1001",
                        Plant = "PL01",
                        OrderQuantity = 20,
                        PurchaseOrderQuantityUnit = "EA",
                        NetPriceAmount = 60.0m,
                        NetAmount = 1200m,
                        DeliveryDate = today.AddDays(15),
                    },
                },
            },
            // 正常：不同物料，第一次出現，無均價可比
            new()
            {
                PurchaseOrder = "4500000013",
                PurchaseOrderType = "NB",
                CompanyCode = "1000",
                PurchasingOrganization = "US01",
                PurchasingGroup = "A20",
                Supplier = "2000000013",
                PurchaseOrderDate = today.AddDays(-8),
                DocumentCurrency = "USD",
                PurchasingCompletenessStatus = true,
                Items = new List<PurchaseOrderItem>
                {
                    new()
                    {
                        PurchaseOrder = "4500000013",
                        PurchaseOrderItemNumber = "00010",
                        Material = "MAT-2002",
                        Plant = "PL02",
                        OrderQuantity = 30,
                        PurchaseOrderQuantityUnit = "EA",
                        NetPriceAmount = 40.0m,
                        NetAmount = 1200m,
                        DeliveryDate = today.AddDays(25),
                    },
                },
            },
            // 正常：同上物料，單價接近，不觸發異常
            new()
            {
                PurchaseOrder = "4500000014",
                PurchaseOrderType = "NB",
                CompanyCode = "1000",
                PurchasingOrganization = "US01",
                PurchasingGroup = "A20",
                Supplier = "2000000013",
                PurchaseOrderDate = today.AddDays(-3),
                DocumentCurrency = "USD",
                PurchasingCompletenessStatus = true,
                Items = new List<PurchaseOrderItem>
                {
                    new()
                    {
                        PurchaseOrder = "4500000014",
                        PurchaseOrderItemNumber = "00010",
                        Material = "MAT-2002",
                        Plant = "PL02",
                        OrderQuantity = 30,
                        PurchaseOrderQuantityUnit = "EA",
                        NetPriceAmount = 41.0m,
                        NetAmount = 1230m,
                        DeliveryDate = today.AddDays(18),
                    },
                },
            },
            // 正常：另一物料，交期未到，已完成收貨狀態
            new()
            {
                PurchaseOrder = "4500000015",
                PurchaseOrderType = "NB",
                CompanyCode = "1000",
                PurchasingOrganization = "US01",
                PurchasingGroup = "A30",
                Supplier = "2000000014",
                PurchaseOrderDate = today.AddDays(-2),
                DocumentCurrency = "USD",
                PurchasingCompletenessStatus = true,
                Items = new List<PurchaseOrderItem>
                {
                    new()
                    {
                        PurchaseOrder = "4500000015",
                        PurchaseOrderItemNumber = "00010",
                        Material = "MAT-3003",
                        Plant = "PL03",
                        OrderQuantity = 200,
                        PurchaseOrderQuantityUnit = "EA",
                        NetPriceAmount = 15.0m,
                        NetAmount = 3000m,
                        DeliveryDate = today.AddDays(10),
                    },
                },
            },
        };
    }
}
