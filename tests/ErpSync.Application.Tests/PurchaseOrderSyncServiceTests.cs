using ErpSync.Domain.Entities;
using static ErpSync.Application.Tests.SyncTestHarness;

namespace ErpSync.Application.Tests;

/// <summary>
/// 對應 spec.md §8 的異常偵測驗收條件與 §9.1 的通知行為。
/// </summary>
public class PurchaseOrderSyncServiceTests
{
    // ---------- 規則一：逾期未交貨 ----------

    [Fact]
    public async Task 交期已過且未完成收貨_應產生一筆Overdue異常()
    {
        var harness = new SyncTestHarness().WithSapOrders(
            SapOrder("4500000001", completenessStatus: false,
                SapItem("4500000001", "10", "MZ-FG-C990", 25m, DaysFromToday(-5))));

        await harness.Service.SyncAsync();

        var anomaly = Assert.Single(harness.Anomalies.Anomalies);
        Assert.Equal(AnomalyRuleType.Overdue, anomaly.RuleType);
        Assert.Equal("4500000001", anomaly.PurchaseOrder);
        Assert.Equal("10", anomaly.PurchaseOrderItemNumber);
    }

    [Fact]
    public async Task 交期已過但已完成收貨_不應產生異常()
    {
        var harness = new SyncTestHarness().WithSapOrders(
            SapOrder("4500000001", completenessStatus: true,
                SapItem("4500000001", "10", "MZ-FG-C990", 25m, DaysFromToday(-5))));

        await harness.Service.SyncAsync();

        Assert.Empty(harness.Anomalies.Anomalies);
    }

    [Fact]
    public async Task 交期未到且未完成收貨_不應產生異常()
    {
        var harness = new SyncTestHarness().WithSapOrders(
            SapOrder("4500000001", completenessStatus: false,
                SapItem("4500000001", "10", "MZ-FG-C990", 25m, DaysFromToday(7))));

        await harness.Service.SyncAsync();

        Assert.Empty(harness.Anomalies.Anomalies);
    }

    // ---------- 規則二：價格異常 ----------

    [Fact]
    public async Task 物料第一次出現_沒有歷史均價可比_不應產生異常()
    {
        var harness = new SyncTestHarness().WithSapOrders(
            SapOrder("4500000001", completenessStatus: false,
                SapItem("4500000001", "10", "MZ-NEW-001", 9999m, DaysFromToday(7))));

        await harness.Service.SyncAsync();

        Assert.Empty(harness.Anomalies.Anomalies);
    }

    [Theory]
    [InlineData(140, "偏高")] // 偏差 +40%
    [InlineData(60, "偏低")]  // 偏差 -40%
    public async Task 單價偏離歷史均價超過三成_應產生一筆PriceVariance異常(decimal netPrice, string _)
    {
        var harness = new SyncTestHarness()
            .WithExistingItem("4500000001", "10", "MZ-FG-C990", netPrice: 100m)
            .WithSapOrders(
                SapOrder("4500000002", completenessStatus: false,
                    SapItem("4500000002", "10", "MZ-FG-C990", netPrice, DaysFromToday(7))));

        await harness.Service.SyncAsync();

        var anomaly = Assert.Single(harness.Anomalies.Anomalies);
        Assert.Equal(AnomalyRuleType.PriceVariance, anomaly.RuleType);
        Assert.Equal("4500000002", anomaly.PurchaseOrder);
        Assert.Contains("40", anomaly.Detail); // Detail 需帶出偏差百分比（spec.md §9.1 信件內容）
    }

    [Theory]
    [InlineData(120)] // 偏差 20%，門檻內
    [InlineData(130)] // 偏差剛好 30%，規則是「超過」30% 才算，邊界不觸發
    [InlineData(70)]  // 偏差剛好 -30%
    public async Task 單價偏離未超過三成_不應產生異常(decimal netPrice)
    {
        var harness = new SyncTestHarness()
            .WithExistingItem("4500000001", "10", "MZ-FG-C990", netPrice: 100m)
            .WithSapOrders(
                SapOrder("4500000002", completenessStatus: false,
                    SapItem("4500000002", "10", "MZ-FG-C990", netPrice, DaysFromToday(7))));

        await harness.Service.SyncAsync();

        Assert.Empty(harness.Anomalies.Anomalies);
    }

    [Fact]
    public async Task 同一批同步中的第二筆同物料_應以第一筆單價為比較基準()
    {
        // 兩筆品項都是這次同步才進來的：第一筆是該物料第一次出現（不判斷），
        // 第二筆要能拿第一筆的 100 元當均價，算出 100% 偏差
        var harness = new SyncTestHarness().WithSapOrders(
            SapOrder("4500000001", completenessStatus: false,
                SapItem("4500000001", "10", "MZ-FG-C990", 100m, DaysFromToday(7)),
                SapItem("4500000001", "20", "MZ-FG-C990", 200m, DaysFromToday(7))));

        await harness.Service.SyncAsync();

        var anomaly = Assert.Single(harness.Anomalies.Anomalies);
        Assert.Equal(AnomalyRuleType.PriceVariance, anomaly.RuleType);
        Assert.Equal("20", anomaly.PurchaseOrderItemNumber);
    }

    // ---------- 重複同步與 SyncLog ----------

    [Fact]
    public async Task 已存在的品項再次同步_不應重複寫入也不應重複產生異常()
    {
        var harness = new SyncTestHarness().WithSapOrders(
            SapOrder("4500000001", completenessStatus: false,
                SapItem("4500000001", "10", "MZ-FG-C990", 25m, DaysFromToday(-5))));

        await harness.Service.SyncAsync();
        var secondRun = await harness.Service.SyncAsync();

        Assert.Single(harness.PurchaseOrders.AllItems);
        Assert.Single(harness.Anomalies.Anomalies);
        Assert.Equal(0, secondRun.RecordsNew);
        Assert.Equal(0, secondRun.AnomaliesFound);
    }

    [Fact]
    public async Task 每次同步應寫入一筆SyncLog且筆數與實際結果一致()
    {
        // 3 筆抓取：1 筆逾期（觸發 Overdue）、2 筆正常
        var harness = new SyncTestHarness().WithSapOrders(
            SapOrder("4500000001", completenessStatus: false,
                SapItem("4500000001", "10", "MZ-FG-C990", 25m, DaysFromToday(-5)),
                SapItem("4500000001", "20", "MZ-FG-A100", 30m, DaysFromToday(7))),
            SapOrder("4500000002", completenessStatus: true,
                SapItem("4500000002", "10", "MZ-FG-B200", 40m, DaysFromToday(-1))));

        var result = await harness.Service.SyncAsync();

        var syncLog = Assert.Single(harness.SyncLogs.SyncLogs);
        Assert.Equal(3, syncLog.RecordsFetched);
        Assert.Equal(3, syncLog.RecordsNew);
        Assert.Equal(1, syncLog.AnomaliesFound);
        Assert.Equal(new(3, 3, 1), result);
    }

    // ---------- 通知（spec.md §9.1）----------

    [Fact]
    public async Task 通知寄送成功_應回寫NotifiedAt()
    {
        var harness = new SyncTestHarness().WithSapOrders(
            SapOrder("4500000001", completenessStatus: false,
                SapItem("4500000001", "10", "MZ-FG-C990", 25m, DaysFromToday(-5))));

        await harness.Service.SyncAsync();

        Assert.Single(harness.Notifier.NotifiedAnomalies);
        Assert.All(harness.Anomalies.Anomalies, a => Assert.NotNull(a.NotifiedAt));
    }

    [Fact]
    public async Task 通知寄送失敗_同步仍應成功且NotifiedAt維持null等下次補寄()
    {
        var harness = new SyncTestHarness().WithSapOrders(
            SapOrder("4500000001", completenessStatus: false,
                SapItem("4500000001", "10", "MZ-FG-C990", 25m, DaysFromToday(-5))));
        harness.Notifier.ShouldSucceed = false;

        var result = await harness.Service.SyncAsync();

        Assert.Equal(1, result.AnomaliesFound);
        Assert.All(harness.Anomalies.Anomalies, a => Assert.Null(a.NotifiedAt));

        // 下次同步沒有新資料，但未通知的異常要被補寄出去
        harness.Notifier.ShouldSucceed = true;
        await harness.Service.SyncAsync();

        Assert.Single(harness.Notifier.NotifiedAnomalies);
        Assert.All(harness.Anomalies.Anomalies, a => Assert.NotNull(a.NotifiedAt));
    }
}
