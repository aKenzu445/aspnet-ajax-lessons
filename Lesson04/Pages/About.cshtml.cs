using Lesson1.WebServer.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lesson1.WebServer.Pages;

// === дз зан3: страница о сервере ===
// то же что /api/server-info, но страницу целиком собирает сервер:
// OnGet заполняет свойства, razor вставляет в html. ни одного ajax
public class AboutModel : PageModel
{
    public string ServerName { get; set; } = "";
    public string Version { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime CurrentTime { get; set; }
    public TimeSpan Uptime { get; set; }      // TimeSpan - промежуток времени

    public void OnGet()
    {
        ServerName = ServerInfo.Name;
        Version = ServerInfo.Version;
        StartedAt = ServerInfo.StartedAt;
        CurrentTime = DateTime.Now;
        Uptime = CurrentTime - StartedAt;     // разница двух дат - это TimeSpan

        ViewData["Description"] = "Имя, версия и время работы сервера (страница без AJAX)";   // зан4 зад10
    }
}
