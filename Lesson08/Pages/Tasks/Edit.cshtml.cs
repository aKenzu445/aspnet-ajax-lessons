using Lesson1.WebServer.Data;
using Lesson1.WebServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;   // SelectList - варианты для <select>
using Microsoft.EntityFrameworkCore;

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

    // дз зан6 -> зан8 зад5: варианты для выпадающего списка категорий, теперь из таблицы Categories (value = Id, текст = Name)
    public SelectList CategoryOptions { get; set; } = new(Array.Empty<Category>());

    private async Task LoadCategoryOptionsAsync()
    {
        CategoryOptions = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
    }

    // GET /Tasks/Edit/5 -> id = 5 (из маршрута {id:int})
    public async Task<IActionResult> OnGetAsync(int id)
    {
        TaskItem? task = await _db.Tasks.FindAsync(id);

        // === зан6 зад10: протухшая ссылка, задачи с таким id уже нет ===
        // без проверки Model.Task был бы null и страница упала бы с 500
        // вместо этого норм сообщение (TempData["Error"] из зан4) и редирект
        if (task == null)
        {
            TempData["Error"] = "Задача не найдена, возможно, она уже удалена";
            return RedirectToPage("/Tasks");
        }

        Task = task;
        SetDescription();
        await LoadCategoryOptionsAsync();
        return Page();   // показать страницу с формой
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
        // есть ошибки - НЕ сохраняю и показываю ту же форму без редиректа, asp-validation-for пишет ошибку
        // === зан7 зад10: пробелы по краям - сначала Trim() потом проверяю заново ===
        // (без этого "  аб  " сохранялось с пробелами и проходило минимум 3 символа)
        Task.Title = (Task.Title ?? "").Trim();
        ModelState.ClearValidationState(nameof(Task));
        if (string.IsNullOrWhiteSpace(Task.Title))
        {
            ModelState.AddModelError("Task.Title", "Название обязательно");
        }
        else
        {
            TryValidateModel(Task, nameof(Task));
        }

        // === зан7 зад9: правило на всю форму (зависит от двух полей) ===
        // если задача выполнена - у нее обязано быть название
        // это ошибка не одного поля а всей формы, поэтому ключ пустой ""
        // такие ошибки показывает <div asp-validation-summary="All">, а не <span asp-validation-for>
        // проверка стоит ДО return Page(): при пустом Title ModelState уже кривой из-за [Required] и сюда бы не дошли
        if (Task.IsDone && string.IsNullOrWhiteSpace(Task.Title))
        {
            ModelState.AddModelError("", "Нельзя отметить выполненной задачу без названия");
        }

        if (!ModelState.IsValid)
        {
            SetDescription();
            await LoadCategoryOptionsAsync();
            return Page();
        }

        // ищу задачу в базе ЗАНОВО по Task.Id (из скрытого поля формы)
        // объекта из OnGetAsync уже нет: каждый запрос - НОВЫЙ EditModel и НОВЫЙ TaskDbContext
        TaskItem? existing = await _db.Tasks.FindAsync(Task.Id);

        // зад10: задачу могли удалить пока форма была открыта
        if (existing == null)
        {
            TempData["Error"] = "Задача не найдена, возможно, она уже удалена";
            return RedirectToPage("/Tasks");
        }

        // переношу новые значения из формы в объект из базы
        existing.Title = Task.Title;
        existing.IsDone = Task.IsDone;
        // дз: категория из списка, "Без категории" (пусто) -> null
        existing.CategoryId = Task.CategoryId;   // зан8: номер категории из списка
        existing.DueDate = Task.DueDate;   // дз зан7: срок

        await _db.SaveChangesAsync();   // EF сделает UPDATE тока измененных столбцов

        // как в зан4: сообщение через TempData + редирект (PRG)
        TempData["Message"] = "Задача обновлена!";
        return RedirectToPage("/Tasks");
    }
}
