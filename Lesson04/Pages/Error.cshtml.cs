using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lesson1.WebServer.Pages;

// зан3 зад9: модель страницы ошибки
// [ResponseCache(NoStore = true)] - браузер не кэширует ошибку
// [IgnoreAntiforgeryToken] - страница может открыться после упавшего POST, токен тут не нужен
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    // номер запроса - по нему можно найти ошибку в логах
    public string? RequestId { get; set; }

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        ViewData["Description"] = "Не удалось обработать запрос";   // зан4 зад10
    }
}
