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
    }
}
