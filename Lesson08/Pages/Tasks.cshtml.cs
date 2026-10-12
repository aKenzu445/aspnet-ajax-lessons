using Lesson1.WebServer.Data;
using Lesson1.WebServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;   // SelectList - варианты для <select>
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

    // === зан8 зад9: пагинация ===
    // const - константа, 5 задач на страницу (удобно проверять, обычно 10-20)
    public const int PageSize = 5;
    public int CurrentPage { get; set; } = 1;   // номер открытой страницы (с 1)
    public int TotalPages { get; set; }         // сколько всего страниц
    public int TotalCount { get; set; }         // сколько задач подходит под фильтр

    // дз зан5 -> зан8: все категории из таблицы Categories (для фильтра) и какая выбрана (null - все задачи)
    public List<Category> AllCategories { get; set; } = new();
    public int? SelectedCategoryId { get; set; }

    // зан8 зад5: варианты выпадающего списка категорий для формы добавления
    // SelectList(список, "Id", "Name"): value у <option> = Id, текст = Name
    // (в задании ViewBag.Categories, но взял свойство с типом - как выяснили в зан4, компилятор проверяет имя и тип)
    public SelectList CategoryOptions { get; set; } = new(Array.Empty<Category>());

    // === зан6 зад8: новая задача из обычной формы ===
    // вместо OnPost(string title) теперь привязка модели [BindProperty]:
    // поля asp-for="NewTask.Title" / "NewTask.CategoryId" заполняют этот объект,
    // а [Required]/[StringLength] с TaskItem.Title проверяются сами
    [BindProperty]
    public TaskItem NewTask { get; set; } = new();

    // OnGet вызывается автоматически при GET-запросе страницы (/Tasks).
    // Занятие 5: стал асинхронным (async Task), потому что читает из базы.
    // ДЗ занятия 5: параметр category приходит из адреса /Tasks?category=Работа (форма фильтра).
    // Задача 9: номер страницы приходит из адреса /Tasks?pageNumber=2 (по умолчанию 1).
    // ВАЖНО: параметр НЕ может называться page (как в тексте задания). В Razor Pages "page" —
    // зарезервированное значение маршрута (путь самой страницы, "/Tasks"). Параметр int page
    // получал бы "/Tasks" вместо номера и всегда оставался 1, а asp-route-page не добавлял бы
    // номер в ссылку. Поэтому — pageNumber.
    public async Task OnGetAsync(int? categoryId, int pageNumber = 1)
    {
        await LoadPageDataAsync(categoryId, pageNumber);
    }

    // грузит всё что надо для страницы. зову из OnGetAsync и из OnPostAsync,
    // когда форма не прошла проверку и страница показывается сразу без редиректа (OnGetAsync тогда не вызывается)
    private async Task LoadPageDataAsync(int? categoryId, int pageNumber)
    {
        // зан5 зад10: читаю из базы, тот же источник что у GET /api/tasks
        // зан8 зад6: EF НЕ тянет связанные данные сам - без Include у задачи Category == null, хотя CategoryId есть
        // .Include(t => t.Category) грузит категорию вместе с задачами одним запросом (JOIN)
        // это "жадная загрузка" (eager loading), заодно нет проблемы N+1
        // зад7: AsQueryable - запрос еще не выполнен, можно докидывать условия
        IQueryable<TaskItem> query = _db.Tasks.Include(t => t.Category).AsQueryable();
        // зад7: Where добавляю тока если категорию выбрали
        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId);
        }
        // зад9: сначала считаю СКОЛЬКО задач подходит под фильтр (до Skip/Take)
        // CountAsync - отдельный SELECT COUNT(*) с тем же WHERE
        TotalCount = await query.CountAsync();
        // (double) чтоб делилось с дробью: 12/5.0 = 2.4, Ceiling округляет вверх -> 3 страницы
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        CurrentPage = pageNumber < 1 ? 1 : pageNumber;   // защита от 0 и минусов

        // Порядок LINQ важен: Where (фильтр, выше) → OrderBy (сортировка) → Skip/Take (страница).
        // Иначе "страница" была бы из неотсортированных или нефильтрованных данных.
        InitialTasks = await query
            .OrderByDescending(t => t.CreatedAt)          // пока сортировка по дате создания (новые сверху)
            .Skip((CurrentPage - 1) * PageSize)          // пропустить задачи прошлых страниц
            .Take(PageSize)                              // взять не больше 5
            .ToListAsync();                              // <- тока тут запрос уходит в базу
        // если страница левая (pageNumber=99) - Skip всё пропустит, будет пустой список, без 500
        SelectedCategoryId = categoryId;

        // зан8: категории из СВОЕЙ таблицы, не надо собирать их по задачам
        AllCategories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        CategoryOptions = new SelectList(AllCategories, "Id", "Name");

        // зан4 зад2: описание пишу через ViewData (словарь ключ -> значение)
        // его можно читать и как ViewBag.Description - ViewBag это не отдельная штука,
        // а dynamic обертка над тем же ViewData, разница тока в синтаксисе
        // зад10: по общему правилу ключ "Description", выводится в _Layout
        ViewData["Description"] = "Здесь вы можете управлять своими задачами";
    }

    // === зан4 зад3 -> зан6 зад8: обычная форма добавления ===
    // OnPost срабатывает на POST /Tasks (когда жмут submit в <form method="post">)
    // с зан6 данные прилетают в NewTask ([BindProperty]), параметров у метода нет
    // Task<IActionResult> - результат (страница, редирект...), метод асинхронный
    public async Task<IActionResult> OnPostAsync()
    {
        // зан6 зад8: та же проверка что на странице редактирования (зад7) - по атрибутам TaskItem
        // ошибка - показываю страницу сразу (без редиректа), текст ошибки у поля
        // === зан7 зад10: пробелы по краям ===
        // атрибуты проверили ДО обрезки: "  аб  " (6 символов) прошло MinimumLength = 3, а сохранилось бы "аб"
        // поэтому сначала Trim(), потом проверяю заново
        NewTask.Title = (NewTask.Title ?? "").Trim();
        ModelState.ClearValidationState(nameof(NewTask));        // забыть результаты проверки до Trim
        if (string.IsNullOrWhiteSpace(NewTask.Title))            // своя проверка "пусто после Trim"
        {
            ModelState.AddModelError("NewTask.Title", "Название обязательно");
        }
        else
        {
            TryValidateModel(NewTask, nameof(NewTask));         // проверяю все атрибуты еще раз
        }

        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync(null, 1);
            return Page();
        }

        // зан5 зад10: сохраняю в базу (Id даст база)
        _db.Tasks.Add(new TaskItem
        {
            Title = NewTask.Title,       // уже обрезан (зан7 зад10)
            IsDone = false,
            CreatedAt = DateTime.Now,    // зан5 зад11: дата создания
            CategoryId = NewTask.CategoryId,   // зан8 зад5: номер категории из списка
            DueDate = NewTask.DueDate    // дз зан7 (уже проверено атрибутом [MustBeFutureOrToday])
        });
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
