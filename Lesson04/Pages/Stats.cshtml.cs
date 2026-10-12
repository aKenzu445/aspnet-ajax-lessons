using Lesson1.WebServer.Data;
using Lesson1.WebServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lesson1.WebServer.Pages;

// === зан4 зад7: статистика по-нормальному, через модель ===
// подсчет перенес сюда из Stats.cshtml в OnGet
// свойства типа int - компилятор проверяет имена и типы, rider подсказывает
public class StatsModel : PageModel
{
    public int TotalCount { get; set; }
    public int DoneCount { get; set; }
    public int UndoneCount { get; set; }

    public void OnGet()
    {
        List<TaskItem> tasks = TaskStorage.GetAll();

        TotalCount = tasks.Count;
        DoneCount = tasks.Count(t => t.IsDone);       // LINQ: сколько штук где IsDone == true
        UndoneCount = tasks.Count(t => !t.IsDone);

        // зад10: описание страницы - по общему правилу в OnGet()
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
