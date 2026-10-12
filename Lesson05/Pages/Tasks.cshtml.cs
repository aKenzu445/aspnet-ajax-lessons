using Lesson1.WebServer.Data;
using Lesson1.WebServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lesson1.WebServer.Pages;

// === зан3 зад5: PageModel (код страницы /Tasks) ===
// TasksModel наследуется от PageModel (": PageModel")
// выполняется на серваке ДО того как соберется html, в .cshtml доступен как Model
public class TasksModel : PageModel
{
    // === зан5 зад10: база через DI ===
    // asp.net видит что конструктору нужен TaskDbContext и сам его дает (зарегал в Program.cs через AddDbContext)
    // сохраняю в поле _db
    private readonly TaskDbContext _db;

    public TasksModel(TaskDbContext db)
    {
        _db = db;
    }

    // стартовый список задач для серверной отрисовки (SSR)
    public List<TaskItem> InitialTasks { get; set; } = new();

    // OnGet вызывается автоматически при GET-запросе страницы (/Tasks).
    // Занятие 5: стал асинхронным (async Task), потому что читает из базы.
    public async Task OnGetAsync()
    {
        // Занятие 5, задача 10: читаем из БАЗЫ — тот же источник, что у GET /api/tasks
        InitialTasks = await _db.Tasks.ToListAsync();

        // зан4 зад2: описание пишу через ViewData (словарь ключ -> значение)
        // его можно читать и как ViewBag.Description - ViewBag это не отдельная штука,
        // а dynamic обертка над тем же ViewData, разница тока в синтаксисе
        // зад10: по общему правилу ключ "Description", выводится в _Layout
        ViewData["Description"] = "Здесь вы можете управлять своими задачами";
    }

    // === зан4 зад3: обработчик обычной формы добавления ===
    // OnPost срабатывает на POST /Tasks (submit в <form method="post">)
    // title берется из поля формы с name="title"
    // Task<IActionResult> - то же что IActionResult, тока метод async (результат будет потом)
    public async Task<IActionResult> OnPostAsync(string title)
    {
        // === зан4 зад9: проверка обычной формы ===
        // пустое название или одни пробелы - не добавляю, пишу ошибку в TempData и обратно на страницу
        //
        // сравнение двух подходов:
        // - ajax (POST /api/tasks): сервак отдает 400 + json, js сам показывает ошибку, страница не перезагружается.
        //   норм для api и интерактивных страниц, ответ поймет любой клиент
        // - обычная форма: после редиректа браузер получает 200 и новую страницу, ошибка через TempData.
        //   норм когда надо чтоб работало без js и "отправил форму - получил страницу"
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Название задачи не может быть пустым";
            return RedirectToPage();
        }

        // зан5 зад10: сохраняю в базу (Id даст база)
        _db.Tasks.Add(new TaskItem { Title = title.Trim(), IsDone = false });
        await _db.SaveChangesAsync();

        // зан4 зад4: TempData - данные которые переживают ровно один редирект
        // ViewData тут не катит: живет один запрос, а после редиректа будет НОВЫЙ запрос
        // TempData лежит в cookie до следующего запроса и удаляется после чтения
        TempData["Message"] = "Задача успешно добавлена!";

        // Post-Redirect-Get (PRG): после POST отвечаю редиректом (302) на GET /Tasks
        // браузер сам перезагрузит страницу, и если нажать F5 - будет безопасный GET, а не повторная отправка формы (дубля не будет)
        return RedirectToPage();
    }

    // === зан4 зад5: удаление обычной формой ===
    // именованный обработчик: OnPost + "Delete". его вызывает форма с asp-page-handler="Delete"
    // (в адресе ?handler=Delete), так можно несколько OnPost на одной странице
    // id прилетает из asp-route-id (в адресе &id=3). Async в конце имени не мешает
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        // зан5 зад10: удаляю из базы
        TaskItem? task = await _db.Tasks.FindAsync(id);

        if (task != null)
        {
            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            TempData["Message"] = $"Задача №{id} удалена";
        }
        else
        {
            TempData["Error"] = $"Задача №{id} не найдена";
        }

        return RedirectToPage();
    }
}
