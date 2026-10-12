using Lesson1.WebServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lesson1.WebServer.Pages;

// === зан4 зад7: статистика по-нормальному, через модель ===
// подсчет перенес сюда из Stats.cshtml в OnGet
// свойства типа int - компилятор проверяет имена и типы, rider подсказывает
public class StatsModel : PageModel
{
    // зан5 зад10: база через конструктор (DI)
    private readonly TaskDbContext _db;

    public StatsModel(TaskDbContext db)
    {
        _db = db;
    }

    public int TotalCount { get; set; }
    public int DoneCount { get; set; }
    public int UndoneCount { get; set; }

    public async Task OnGetAsync()
    {
        // зан5 зад10: считаю прям в базе. CountAsync станет SELECT COUNT(*) FROM Tasks WHERE ...
        // все задачи в память не тянутся
        TotalCount = await _db.Tasks.CountAsync();
        DoneCount = await _db.Tasks.CountAsync(t => t.IsDone);       // где IsDone == true
        UndoneCount = await _db.Tasks.CountAsync(t => !t.IsDone);

        // зан4 зад10: описание страницы - по общему правилу в OnGet()
        ViewData["Description"] = "Сколько задач всего, сколько выполнено и сколько осталось";
    }

    // === дз зан4: кнопка обновить статистику ===
    // обычная форма без ajax, OnPost без параметров: пересчет будет на следующем GET (OnGet считает заново),
    // а сообщение доживет после редиректа через TempData
    public IActionResult OnPost()
    {
        TempData["Message"] = $"Статистика обновлена вручную в {DateTime.Now:HH:mm:ss}";
        return RedirectToPage();   // PRG: после F5 форма второй раз не уйдет
    }
}
