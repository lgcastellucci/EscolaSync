using Microsoft.Maui.Graphics;

namespace EscolaSync.Models;

public class LogEntry
{
    public string Icon { get; set; } = "▸";
    public string Time { get; set; } = DateTime.Now.ToString("HH:mm:ss");
    public string Message { get; set; } = "";
    public Color TextColor { get; set; } = Colors.White;
    public Color BgColor { get; set; } = Color.FromArgb("#1E293B");

    public static LogEntry Info(string msg) => new()
    {
        Icon = "ℹ",
        Message = msg,
        TextColor = Color.FromArgb("#93C5FD"),
        BgColor = Color.FromArgb("#1E293B")
    };

    public static LogEntry Ok(string msg) => new()
    {
        Icon = "✓",
        Message = msg,
        TextColor = Color.FromArgb("#86EFAC"),
        BgColor = Color.FromArgb("#052E16")
    };

    public static LogEntry Error(string msg) => new()
    {
        Icon = "✗",
        Message = msg,
        TextColor = Color.FromArgb("#FCA5A5"),
        BgColor = Color.FromArgb("#450A0A")
    };

    public static LogEntry Step(string msg) => new()
    {
        Icon = "▶",
        Message = msg,
        TextColor = Color.FromArgb("#FDE68A"),
        BgColor = Color.FromArgb("#292524")
    };
}
