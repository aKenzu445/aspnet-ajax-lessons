using Lesson1.WebServer.Data;
using Lesson1.WebServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Lesson1.WebServer.Pages.Tasks;

// === зан6 зад3: PageModel страницы редактирования ===
public class EditModel : PageModel
{
    private readonly TaskDbContext _db;

    // база приходит через конструктор (DI), как в зан5
    public EditModel(TaskDbContext db)
    {
        _db = db;
    }

    // [BindProperty] - привязка модели: на POST asp.net сам возьмет поля формы (Task.Id, Task.Title, Task.IsDone)
    // и заполнит объект. никаких Request.Form["..."] руками
    [BindProperty]
    public TaskItem Task { get; set; } = new();

    // GET /Tasks/Edit/5 -> id = 5 (из маршрута {id:int})
    public async Task OnGetAsync(int id)
    {
        // FindAsync вернёт null, если задачи нет (это обрабатывается в задаче 10)
        Task = (await _db.Tasks.FindAsync(id))!;
        SetDescription();
    }

    // описание страницы (правило зан4: Description в PageModel)
    // вынес в метод, потому что страница показывается и из OnGet, и из OnPost (при ошибке)
    private void SetDescription()
    {
        ViewData["Description"] = "Измените название или отметьте задачу выполненной";
    }

    // === зан6 зад5: сохраняю изменения ===
    // POST /Tasks/Edit/5. Task уже заполнен из формы благодаря [BindProperty]
    public async Task<IActionResult> OnPostAsync()
    {
        // === зан6 зад7: проверка на серваке ===
        // ModelState - результат проверки атрибутов [Required], [StringLength]...
        // есть ошибки - НЕ сохраняю и показываю ту же форму без редиректа:
        // введенное остается в полях, а asp-validation-for пишет ошибку
        if (!ModelState.IsValid)
        {
            SetDescription();
            return Page();
        }

        // ищу задачу в базе ЗАНОВО по Task.Id (из скрытого поля формы)
        // объекта из OnGetAsync уже нет: каждый запрос - НОВЫЙ EditModel и НОВЫЙ TaskDbContext
        TaskItem? existing = await _db.Tasks.FindAsync(Task.Id);

        // переношу новые значения из формы в объект из базы
        existing!.Title = Task.Title;
        existing.IsDone = Task.IsDone;

        await _db.SaveChangesAsync();   // EF сделает UPDATE тока измененных столбцов

        // как в зан4: сообщение через TempData + редирект (PRG)
        TempData["Message"] = "Задача обновлена!";
        return RedirectToPage("/Tasks");
    }
}
