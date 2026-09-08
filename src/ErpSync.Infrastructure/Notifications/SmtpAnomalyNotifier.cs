using ErpSync.Application.Interfaces;
using ErpSync.Domain.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ErpSync.Infrastructure.Notifications;

/// <summary>
/// 用 MailKit 把異常寄到本機 SMTP 測試工具（Mailpit/Papercut）。spec.md §9.1
/// </summary>
public class SmtpAnomalyNotifier : IAnomalyNotifier
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpAnomalyNotifier> _logger;

    public SmtpAnomalyNotifier(IOptions<SmtpOptions> options, ILogger<SmtpAnomalyNotifier> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> NotifyAsync(Anomaly anomaly, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Smtp:Enabled = false，跳過異常 {AnomalyId} 的通知", anomaly.Id);
            return false;
        }

        var message = BuildMessage(anomaly);

        try
        {
            using var client = new SmtpClient();
            // 本機測試 SMTP（Mailpit/Papercut）沒有 TLS，用 None；正式環境改設定即可
            await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.None, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            _logger.LogInformation(
                "已寄出異常通知：Anomaly {AnomalyId}（{RuleType} / PO {PurchaseOrder}-{ItemNumber}）",
                anomaly.Id, anomaly.RuleType, anomaly.PurchaseOrder, anomaly.PurchaseOrderItemNumber);
            return true;
        }
        catch (Exception ex)
        {
            // 通知失敗不能讓同步失敗；NotifiedAt 維持 null，下次同步會自動補寄（spec.md §9.1）
            _logger.LogWarning(ex, "寄送異常通知失敗：Anomaly {AnomalyId}，將於下次同步重試", anomaly.Id);
            return false;
        }
    }

    private MimeMessage BuildMessage(Anomaly anomaly)
    {
        var ruleName = anomaly.RuleType == AnomalyRuleType.Overdue ? "逾期未交貨" : "價格異常";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.From));
        message.To.Add(MailboxAddress.Parse(_options.To));
        message.Subject = $"[ERP 異常通知] {ruleName} — 採購單 {anomaly.PurchaseOrder} 品項 {anomaly.PurchaseOrderItemNumber}";
        message.Body = new TextPart("plain")
        {
            Text = $"""
                    異常類型：{ruleName}（{anomaly.RuleType}）
                    採購單號：{anomaly.PurchaseOrder}
                    品項編號：{anomaly.PurchaseOrderItemNumber}
                    觸發原因：{anomaly.Detail}
                    偵測時間：{anomaly.DetectedAt:yyyy-MM-dd HH:mm:ss} (UTC)

                    本信件由 ErpSync 同步排程自動發送。
                    """,
        };

        return message;
    }
}
