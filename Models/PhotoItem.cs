namespace EscolaSync.Models;

public class PhotoItem
{
    public long Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = "image/jpeg";
    public long SizeBytes { get; set; }
    public DateTime DateTaken { get; set; }

    public string SizeDisplay => SizeBytes < 1024 * 1024
        ? $"{SizeBytes / 1024} KB"
        : $"{SizeBytes / (1024.0 * 1024):F1} MB";
}
