namespace EscolaSync.Models;

public class SyncResult
{
    public int Total { get; set; }
    public int Uploaded { get; set; }
    public int Deleted { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();

    public bool HasErrors => Errors.Count > 0;
    public string Summary => $"{Uploaded} enviada(s), {Failed} falha(s), {Deleted} removida(s) do celular";
}
