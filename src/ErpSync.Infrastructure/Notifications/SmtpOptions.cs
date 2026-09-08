namespace ErpSync.Infrastructure.Notifications;

/// <summary>對應設定檔的 Smtp 節點（spec.md §9.1）。</summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    /// <summary>false 時整個通知流程跳過（測試或無 SMTP 環境用）。</summary>
    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string From { get; set; } = "erpsync@example.com";
    public string To { get; set; } = "mis@example.com";
}
