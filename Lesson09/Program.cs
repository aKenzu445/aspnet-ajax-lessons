// занятие 9. REST API: DTO, сериализация и норм ajax клиент (продолжаю проект с зан1-8)
using System.ComponentModel.DataAnnotations; // Validator, ValidationResult (зан7 зад7)
using System.Diagnostics;          // Stopwatch - секундомер (зан2 зад3)
using System.Globalization;
using System.Text.Encodings.Web;   // TextEncoderSettings (кириллица в razor)
using System.Text.Unicode;         // UnicodeRanges
using Microsoft.Extensions.WebEncoders; // WebEncoderOptions
using Microsoft.EntityFrameworkCore;   // UseSqlite, ToListAsync и тд (зан5)
using Lesson1.WebServer.Api;      // ApiError - общий формат ошибок (зан9)
using Lesson1.WebServer.Data;     // TaskDbContext - база, ServerInfo (папка Data)
using Lesson1.WebServer.Dtos;     // DTO для api (зан9)
using Lesson1.WebServer.Middleware; // ApiKeyMiddleware (папка Middleware)
using Lesson1.WebServer.Models;   // TaskItem - класс задачи (папка Models)

// builder - тут настраиваю сервисы и конфиг
var builder = WebApplication.CreateBuilder(args);

// === зан3 зад1: подключаю razor pages ===
// AddRazorPages регает всё что нужно чтоб .cshtml превращались в html
builder.Services.AddRazorPages();

// зан9 зад6: если параметры кривые (?a=abc для double, сломанный json) minimal api всегда кидает BadHttpRequestException,
// а мой middleware делает из него 400 в общем формате. без этой настройки вне Development был бы пустой 400
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

// === зан5 зад4: регаю DbContext в DI ===
// AddDbContext - asp.net сам создает TaskDbContext на каждый запрос и отдает куда надо (endpoint или PageModel)
// UseSqlite - какая бд, строка подключения из appsettings.json -> ConnectionStrings:TaskDb
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TaskDb")));

// razor по умолчанию превращает все не-латинские буквы в коды (И -> &#x418;)
// в браузере норм, но в "просмотре кода" русский не прочитать. разрешаю выводить всё как есть
// (защита от html-инъекций остается: < > & " всё равно экранируются)
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// app - само приложение (внутри сервак Kestrel)
var app = builder.Build();

// дз зан3: запоминаю когда запустился сервак, для страницы о сервере
ServerInfo.StartedAt = DateTime.Now;

// зан5 (чтоб удобно запускать): на старте применяю миграции которых еще нет в базе
// то же самое что "dotnet ef database update". tasks.db в гит не кладу, поэтому после скачивания база создастся сама
// CreateScope - временная "область" чтоб достать сервисы (DbContext живет в пределах запроса/области)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    db.Database.Migrate();
}

// === зан3 зад9: ошибки для обычных страниц сайта ===
// стоит первым. если страница упала - запрос перекидывается на /Error и юзер видит Pages/Error.cshtml
app.UseExceptionHandler("/Error");

// === зан2 зад8: ловлю ошибки для api ===
// оборачиваю всё что дальше в try/catch, вылетело исключение -> норм json 500 вместо страшной страницы
// зан3 зад9: ловлю ТОКА /api/... - "when" пропускает остальное наверх в UseExceptionHandler,
// он покажет html страницу. итого у api и у сайта разная обработка ошибок и обе работают
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    // зан9 зад6: кривой запрос (не тот тип параметра, сломанный json)
    // minimal api сообщает об этом через BadHttpRequestException (см. ThrowOnBadRequest выше)
    catch (BadHttpRequestException ex) when (context.Request.Path.StartsWithSegments("/api") && !context.Response.HasStarted)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Некорректный запрос {context.Request.Path}: {ex.Message}");
        context.Response.Clear();
        await ApiError.WriteAsync(context, StatusCodes.Status400BadRequest, "Некорректные параметры запроса", ApiError.InvalidParameters);
    }
    catch (Exception ex) when (context.Request.Path.StartsWithSegments("/api"))
    {
        // полная инфа об ошибке - тока в консоль сервака, для меня
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ОШИБКА при {context.Request.Method} {context.Request.Path}:");
        Console.WriteLine(ex);

        // если ответ уже начал уходить клиенту - поменять уже нельзя
        if (context.Response.HasStarted)
        {
            throw;
        }

        context.Response.Clear();
        // 500 в общем формате (зан9 зад6)
        await ApiError.WriteAsync(context, StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", ApiError.InternalError);
    }
});

// === зан2 зад2-3: логирую каждый запрос ===
// app.Use(...) - inline middleware (способ 1). context - всё про запрос и ответ, next - следующий в цепочке
// стоит в самом начале, до UseStaticFiles, поэтому видит и запросы к файлам
app.Use(async (context, next) =>
{
    // зад2: что пришло. QueryString - всё что после "?" (типа ?a=10&b=0)
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] → {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");

    // зад3: засекаю сколько обрабатывается запрос
    var stopwatch = Stopwatch.StartNew();

    await next();   // пускаю запрос дальше по цепочке (без этого он тут застрянет)

    // сюда возвращаемся когда ответ уже готов (остальные middleware и endpoint отработали)
    stopwatch.Stop();
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ← {context.Response.StatusCode} ({stopwatch.ElapsedMilliseconds} ms)");
});

// === зан1 зад4: статика из wwwroot ===
// UseStaticFiles отдает css/js как есть, без него /css/site.css = 404
// зан3 зад2: UseDefaultFiles выкинул - главную "/" теперь рисует razor (Pages/Index.cshtml), старый index.html удалил
app.UseStaticFiles();

// === зан2 зад10: режим техработ ===
// пришел заголовок X-Maintenance: true -> сразу 503, дальше ничего не выполняется
// стоит после логов (чтоб в лог попало) и после UseStaticFiles (статику не блочим, так в задании)
// но до UseRouting и всех endpoints
app.Use(async (context, next) =>
{
    if (context.Request.Headers["X-Maintenance"] == "true")
    {
        // 503 в общем формате (зан9 зад6)
        await ApiError.WriteAsync(context, StatusCodes.Status503ServiceUnavailable,
            "Сервер временно недоступен, ведутся технические работы", ApiError.Maintenance);
        return;   // next() НЕ зову - дальше запрос не идет
    }

    await next();
});

// === дз зан2: ограничение запросов ===
// много запросов к /api с одного ip -> 429. лимит в appsettings.json (RateLimiting), в зан2 было 5 за 10 сек
app.UseMiddleware<RateLimitingMiddleware>();

// маршрутизация: решает какой endpoint подходит под адрес
// вызвал явно, чтоб было видно - всё что выше выполняется ДО неё
app.UseRouting();

// === зан2 зад7: проверка api ключа для POST/PUT/DELETE ===
// сам класс в папке Middleware. стоит до endpoints, так что без ключа до обработчиков даже не дойдет
app.UseMiddleware<ApiKeyMiddleware>();

// === зад2: первый свой endpoint ===
// GET /api/hello -> просто текст
app.MapGet("/api/hello", () => "Привет! Сервер работает.");

// === зад3: endpoint который отдает json ===
// new { ... } - анонимный объект, asp.net сам делает из него json
// имена сами с маленькой буквы: ServerName -> "serverName"
app.MapGet("/api/server-info", () => new
{
    ServerName = ServerInfo.Name,     // дз зан3: значения из общего класса ServerInfo
    Version = ServerInfo.Version,
    CurrentTime = DateTime.Now        // время считается заново на каждый запрос
});

// === зад5: параметры в адресе (query string), считает сервак ===
// /api/sum?a=5&b=3 -> { "a": 5, "b": 3, "result": 8 }
// a и b asp.net сам берет из адреса по имени, double чтоб и 2.5 и -7 работали
// если чего-то нет или не число - minimal api сам кидает 400 и мой код даже не запускается (это в зад7)
app.MapGet("/api/sum", (double a, double b) => new
{
    A = a,
    B = b,
    Result = a + b
});

// === зад6: шлю данные на сервер через POST ===
// POST /api/echo с телом { "text": "привет" }
// EchoRequest (внизу файла) asp.net заполняет из тела запроса, а не из адреса как в зад5
app.MapPost("/api/echo", (EchoRequest request) =>
{
    string text = request.Text ?? "";   // ?? - если Text не пришел (null), беру пустую строку

    return new
    {
        Original = text,
        Uppercase = text.ToUpper(),
        Length = text.Length
    };
});

// === зад7: ошибки и нормальные коды ответа ===
// GET /api/divide?a=10&b=2
// тут специально string?, а не double - сам проверяю что пришло и отдаю понятную ошибку,
// а не автоматический 400 со страшным текстом как в зад5
app.MapGet("/api/divide", (string? a, string? b) =>
{
    // double.TryParse пробует сделать из строки число: true и число в x (out x), или false если не число
    // InvariantCulture - дробь через точку "2.5"
    bool aOk = double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out double x);
    bool bOk = double.TryParse(b, NumberStyles.Float, CultureInfo.InvariantCulture, out double y);

    if (!aOk || !bOk)   // ! - "не", || - "или"
    {
        return ApiError.BadRequest("Некорректные параметры", ApiError.InvalidParameters);   // 400
    }

    if (y == 0)
    {
        return ApiError.BadRequest("Деление на ноль невозможно", ApiError.DivisionByZero);  // 400
    }

    return Results.Ok(new { Result = x / y });                                  // 200
});

// === зан2 зад9: все endpoints задач в одной группе MapGroup ===
// MapGroup дает общий префикс /api/tasks, внутри пишу тока хвост пути
// адреса не поменялись, поэтому js трогать не пришлось
var tasksApi = app.MapGroup("/api/tasks");

// --- GET /api/tasks - список (зан1 зад8) ---
// с зан5 берется из базы, db подставляется само. в зан8 фильтр по категории
// зан9 зад8: еще поиск, сортировка и страницы, как на /Tasks: Where -> OrderBy -> Skip/Take
// пример: /api/tasks?categoryId=2&search=отчёт&sortBy=title&page=2&pageSize=5
// отдаю "конверт" { items, page, pageSize, totalCount, totalPages }
// (тут page можно, это не razor pages, там page занят)
tasksApi.MapGet("/", async (int? categoryId, string? search, string? sortBy, int? page, int? pageSize, TaskDbContext db) =>
{
    // значения по умолчанию + защита от фигни (страница 0, 1000 задач за раз)
    int currentPage = Math.Max(page ?? 1, 1);
    int size = Math.Clamp(pageSize ?? 10, 1, 50);   // Clamp - зажать в 1..50

    // Include для categoryName в DTO, IQueryable - запрос еще не выполнен
    IQueryable<TaskItem> query = db.Tasks.Include(t => t.Category);

    if (categoryId.HasValue)
    {
        query = query.Where(t => t.CategoryId == categoryId);
    }
    if (!string.IsNullOrWhiteSpace(search))           // поиск (дз зан8, живой поиск зад10)
    {
        // зад10: для проверки гонки тут ВРЕМЕННО стояло
        // await Task.Delay(Math.Max(0, 1500 - search.Length * 400)); (короткий запрос дольше), убрал
        string text = search.Trim();
        query = query.Where(t => t.Title.Contains(text));
    }

    int totalCount = await query.CountAsync();                         // до Skip/Take
    int totalPages = (int)Math.Ceiling(totalCount / (double)size);

    IOrderedQueryable<TaskItem> sorted = sortBy == "title"
        ? query.OrderBy(t => t.Title)
        : query.OrderByDescending(t => t.CreatedAt);

    List<TaskItem> tasks = await sorted
        .ThenBy(t => t.Id)
        .Skip((currentPage - 1) * size)
        .Take(size)
        .ToListAsync();

    // зан9 зад2: наружу DTO, зад8: в "конверте" с инфой о страницах
    var items = tasks.Select(TaskDto.FromEntity).ToList();
    return new PagedResultDto<TaskDto>(items, currentPage, size, totalCount, totalPages);
});

// --- GET /api/tasks/{id} - одна задача (зан2 зад4-5) ---
// /api/tasks/1 -> 200, /api/tasks/99 -> 404
// {id:int} - ограничение маршрута, пускает тока если id число. abc -> 404 сразу, до моего кода
// зан9 зад2: Include + FirstOrDefaultAsync (FindAsync с Include не умеет), отдаю TaskDto
tasksApi.MapGet("/{id:int}", async (int id, TaskDbContext db) =>
{
    TaskItem? task = await db.Tasks
        .Include(t => t.Category)
        .FirstOrDefaultAsync(t => t.Id == id);   // первая подходящая строка или null

    if (task == null)
    {
        return ApiError.NotFound("Задача не найдена", ApiError.TaskNotFound);   // 404
    }

    return Results.Ok(TaskDto.FromEntity(task));                            // 200
});

// --- POST /api/tasks - добавить (зан1 зад9) ---
// шлю { "title": "Купить хлеб" } -> 201 и созданная задача
// зан9 зад3: принимаю CreateTaskDto (там нет Id/IsDone/CreatedAt), отдаю TaskDto
tasksApi.MapPost("/", async (CreateTaskDto request, TaskDbContext db) =>
{
    // зан9 зад9: для проверки индикатора тут ВРЕМЕННО стояло await Task.Delay(2000); (типа медленный сервак), убрал
    // зан5 зад8: создаю БЕЗ Id - база сама даст номер
    var created = new TaskItem
    {
        Title = request.Title?.Trim() ?? "",   // Trim - убрать пробелы по краям, null -> ""
        IsDone = false,
        CreatedAt = DateTime.Now,        // зан5 зад11: дата создания
        // дз зан5: пустую категорию сохраняю как null ("без категории")
        CategoryId = request.CategoryId,      // зан8: номер категории (внешний ключ)
        DueDate = request.DueDate?.Date  // дз зан7: срок (тока дата без времени)
    };

    // === зан7 зад7: проверка в minimal api ===
    // minimal api сам НЕ проверяет атрибуты [Required], [StringLength], [NoForbiddenWords] (это тока ModelState в razor/mvc)
    // поэтому руками: Validator.TryValidateObject гонит все атрибуты и кидает ошибки в results
    // правила те же что у razor страниц, написаны один раз - в TaskItem
    // (ручная проверка IsNullOrWhiteSpace из зан1 больше не нужна, её заменил [Required])
    // зан7 зад10: название уже обрезано Trim() выше, длина считается по реальному значению
    if (string.IsNullOrWhiteSpace(created.Title))
    {
        return ApiError.Validation(new[] { new ValidationResult("Название обязательно", new[] { nameof(TaskItem.Title) }) });
    }

    var results = new List<ValidationResult>();
    bool isValid = Validator.TryValidateObject(created, new ValidationContext(created), results, validateAllProperties: true);

    // зан8: категория с таким номером должна быть
    // AnyAsync - "есть хоть одна такая строка" (SQL EXISTS)
    if (created.CategoryId.HasValue && !await db.Categories.AnyAsync(c => c.Id == created.CategoryId))
    {
        results.Add(new ValidationResult("Категория не найдена", new[] { nameof(TaskItem.CategoryId) }));
        isValid = false;
    }

    if (!isValid)
    {
        // 400 и список ошибок: { "errors": ["Название обязательно", ...] }
        return ApiError.Validation(results);   // 400 VALIDATION_ERROR + поле (дз)
    }

    db.Tasks.Add(created);           // помечаю объект как новый (в базе его еще нет)
    await db.SaveChangesAsync();     // делаю INSERT в базе, после этого у created уже есть Id

    // зан9: для categoryName в ответе подгружаю категорию созданной задачи
    // Entry(...).Reference(...).LoadAsync() - ЯВНАЯ загрузка одной связанной штуки
    await db.Entry(created).Reference(t => t.Category).LoadAsync();

    // === зан9 зад7: REST - 201 Created + заголовок Location ===
    // Results.Created сам ставит 201 и Location: /api/tasks/15 - где лежит созданная задача, в теле DTO
    // (раньше было Results.Json(..., 201) - код норм, но без Location)
    return Results.Created($"/api/tasks/{created.Id}", TaskDto.FromEntity(created));
});

// --- DELETE /api/tasks/{id} (зан1 зад10) ---
// зан5 зад9: удаляю уже из базы
tasksApi.MapDelete("/{id:int}", async (int id, TaskDbContext db) =>
{
    TaskItem? task = await db.Tasks.FindAsync(id);

    if (task == null)
    {
        return ApiError.NotFound("Задача не найдена", ApiError.TaskNotFound);   // 404
    }

    db.Tasks.Remove(task);           // помечаю на удаление
    await db.SaveChangesAsync();     // делаю DELETE в базе

    // зан9 зад7: по REST успешное удаление - 204 No Content без тела (раньше было 200 и { "message": "..." })
    return Results.NoContent();                                             // 204
});

// --- PUT /api/tasks/{id} (дз зан1 -> зан9 зад4) ---
// раньше менял тока isDone, теперь всё сразу: title, isDone, categoryId (UpdateTaskDto)
tasksApi.MapPut("/{id:int}", async (int id, UpdateTaskDto request, TaskDbContext db) =>
{
    TaskItem? task = await db.Tasks.FindAsync(id);

    if (task == null)
    {
        return ApiError.NotFound("Задача не найдена", ApiError.TaskNotFound);   // 404
    }

    // проверяю ТОКА то что меняется. TryValidateProperty гоняет атрибуты одного свойства (Title)
    // весь объект (TryValidateObject) тут не катит: у старой задачи срок мог уже пройти,
    // и [MustBeFutureOrToday] не дал бы даже отметить её выполненной
    string title = request.Title?.Trim() ?? "";
    var results = new List<ValidationResult>();
    var context = new ValidationContext(task) { MemberName = nameof(TaskItem.Title) };
    bool isValid = Validator.TryValidateProperty(title, context, results);

    if (request.CategoryId.HasValue && !await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
    {
        results.Add(new ValidationResult("Категория не найдена", new[] { nameof(TaskItem.CategoryId) }));
        isValid = false;
    }

    if (!isValid)
    {
        return ApiError.Validation(results);   // 400 VALIDATION_ERROR + поле (дз)
    }

    task.Title = title;              // EF следит за изменениями загруженного объекта...
    task.IsDone = request.IsDone;
    task.CategoryId = request.CategoryId;
    await db.SaveChangesAsync();     // ...и на сохранении сделает UPDATE измененных столбцов

    await db.Entry(task).Reference(t => t.Category).LoadAsync();   // для categoryName в ответе
    return Results.Ok(TaskDto.FromEntity(task));                            // 200
});

// === зан9 зад5: список категорий GET /api/categories ===
// -> [ { "id": 1, "name": "Работа", "taskCount": 4 }, ... ]
// Select сразу делает DTO прям в запросе: c.Tasks.Count() станет SELECT COUNT(*), задачи не грузятся, Include не нужен
app.MapGet("/api/categories", async (TaskDbContext db) =>
    await db.Categories
        .OrderBy(c => c.Id)
        .Select(c => new CategoryDto(c.Id, c.Name, c.Tasks.Count()))
        .ToListAsync());

// === зан9 зад6: левый адрес api -> 404 в общем формате ===
// MapFallback срабатывает тока если ни один маршрут не подошел (типа /api/tasks/abc из-за {id:int})
app.MapFallback("/api/{**path}", () =>
    ApiError.NotFound("Такого адреса API не существует", ApiError.RouteNotFound));

// === зан3 зад1: маршруты для всех razor страниц из Pages ===
// адрес берется из имени файла: Pages/Test.cshtml -> /Test, Pages/Index.cshtml -> /
app.MapRazorPages();

// === зан2 зад8: тестовый endpoint который специально падает ===
app.MapGet("/api/crash-test", () =>
{
    throw new Exception("Тестовая ошибка");
});

// запуск сервака: начинает слушать порт и обрабатывать запросы
app.Run();


// === классы (типы данных) ===
// в файле с top-level кодом классы пишутся ПОСЛЕ основного кода

// зад6: тело POST /api/echo. string? - строка которая может быть null, record - короткий класс для данных
public record EchoRequest(string? Text);

// зад9: тело POST /api/tasks
// (зан9 зад3: CreateTaskRequest поменял на Dtos/CreateTaskDto.cs)

// (зан9 зад4: старый UpdateTaskStatusRequest убрал, теперь Dtos/UpdateTaskDto.cs)
