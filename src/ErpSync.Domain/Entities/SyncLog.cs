namespace ErpSync.Domain.Entities;

public class SyncLog
{
    public int Id { get; set; }
    public DateTime RunAt { get; set; }
    public int RecordsFetched { get; set; }
    public int RecordsNew { get; set; }
    public int AnomaliesFound { get; set; }
}
