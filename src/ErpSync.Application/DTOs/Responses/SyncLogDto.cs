namespace ErpSync.Application.DTOs.Responses;

public record SyncLogDto(
    int Id,
    DateTime RunAt,
    int RecordsFetched,
    int RecordsNew,
    int AnomaliesFound);
