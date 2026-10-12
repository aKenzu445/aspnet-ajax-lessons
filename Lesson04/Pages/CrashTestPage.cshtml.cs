using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lesson1.WebServer.Pages;

// зан3 зад9: страница чтоб проверить обработку ошибок на сайте
public class CrashTestPageModel : PageModel
{
    public void OnGet()
    {
        throw new Exception("Тестовая ошибка на Razor-странице");
    }
}
