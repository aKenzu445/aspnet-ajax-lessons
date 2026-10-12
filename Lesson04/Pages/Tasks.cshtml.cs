using Lesson1.WebServer.Data;
using Lesson1.WebServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lesson1.WebServer.Pages;

// === зан3 зад5: PageModel (код страницы /Tasks) ===
// TasksModel наследуется от PageModel (": PageModel")
// выполняется на серваке ДО того как соберется html, в .cshtml доступен как Model
public class TasksModel : PageModel
{
    // стартовый список задач для серверной отрисовки (SSR)
    public List<TaskItem> InitialTasks { get; set; } = new();

    // OnGet срабатывает сам на GET страницы (/Tasks)
    public void OnGet()
    {
        // то же хранилище в памяти что у GET /api/tasks
        InitialTasks = TaskStorage.GetAll();

        // Занятие 4, задача 2. Записываем описание через ViewData (словарь: ключ → значение).
        // В Tasks.cshtml оно читается через ViewBag.PageDescription — и это работает,
        // потому что ViewBag — не отдельное хранилище, а "динамическая обёртка" над тем же
        // словарём ViewData: ViewBag.PageDescription == ViewData["PageDescription"].
        // Разница только в синтаксисе доступа.
        ViewData["PageDescription"] = "Здесь вы можете управлять своими задачами";
    }

    // === зан4 зад3: обработчик обычной формы добавления ===
    // OnPost срабатывает на POST /Tasks (submit в <form method="post">)
    // title берется из поля формы с name="title"
    // IActionResult - что вернуть: страницу, редирект, ошибку и тд
    public IActionResult OnPost(string title)
    {
        TaskStorage.Add(title);

        // Post-Redirect-Get (PRG): после POST отвечаю редиректом (302) на GET /Tasks
        // браузер сам перезагрузит страницу, и если нажать F5 - будет безопасный GET, а не повторная отправка формы (дубля не будет)
        return RedirectToPage();
    }
}
