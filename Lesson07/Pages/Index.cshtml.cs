using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lesson1.WebServer.Pages;

// зан4 зад10: модель главной - нужна чтоб по общему правилу задать описание в OnGet() (раньше у Index её не было)
public class IndexModel : PageModel
{
    public void OnGet()
    {
        ViewData["Description"] = "Проверка API сервера: информация, сумма, echo и деление через AJAX";
    }
}
