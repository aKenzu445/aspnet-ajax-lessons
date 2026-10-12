// занятие 3. razor и layout (продолжаю проект с зан1-2)
using System.Diagnostics;          // Stopwatch - секундомер (зан2 зад3)
using System.Globalization;
using System.Text.Encodings.Web;   // TextEncoderSettings (кириллица в razor)
using System.Text.Unicode;         // UnicodeRanges
using Microsoft.Extensions.WebEncoders; // WebEncoderOptions
using Lesson1.WebServer.Data;     // TaskStorage - хранилище задач (папка Data)
using Lesson1.WebServer.Middleware; // ApiKeyMiddleware (папка Middleware)
using Lesson1.WebServer.Models;   // TaskItem - класс задачи (папка Models)

// builder - тут настраиваю сервисы и конфиг
var builder = WebApplication.CreateBuilder(args);

// === зан3 зад1: подключаю razor pages ===
// AddRazorPages регает всё что нужно чтоб .cshtml превращались в html
builder.Services.AddRazorPages();

// razor по умолчанию превращает все не-латинские буквы в коды (И -> &#x418;)
// в браузере норм, но в "просмотре кода" русский не прочитать. разрешаю выводить всё как есть
// (защита от html-инъекций остается: < > & " всё равно экранируются)
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// app - само приложение (внутри сервак Kestrel)
var app = builder.Build();

// дз зан3: запоминаю когда запустился сервак, для страницы о сервере
ServerInfo.StartedAt = DateTime.Now;

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
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;   // 500
        await context.Response.WriteAsJsonAsync(new { error = "Внутренняя ошибка сервера" });
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
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;   // 503
        await context.Response.WriteAsJsonAsync(new { error = "Сервер временно недоступен, ведутся технические работы" });
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
        return Results.BadRequest(new { Error = "Некорректные параметры" });   // 400
    }

    if (y == 0)
    {
        return Results.BadRequest(new { Error = "Деление на ноль невозможно" }); // 400
    }

    return Results.Ok(new { Result = x / y });                                  // 200
});

// === зан2 зад9: все endpoints задач в одной группе MapGroup ===
// MapGroup дает общий префикс /api/tasks, внутри пишу тока хвост пути
// адреса не поменялись, поэтому js трогать не пришлось
var tasksApi = app.MapGroup("/api/tasks");

// --- GET /api/tasks - список (зан1 зад8) ---
// отдает массив задач в json
tasksApi.MapGet("/", () => TaskStorage.GetAll());

// --- GET /api/tasks/{id} - одна задача (зан2 зад4-5) ---
// /api/tasks/1 -> 200, /api/tasks/99 -> 404
// {id:int} - ограничение маршрута, пускает тока если id число. abc -> 404 сразу, до моего кода
// (без :int на abc было 400 с BadHttpRequestException)
tasksApi.MapGet("/{id:int}", (int id) =>
{
    TaskItem? task = TaskStorage.GetById(id);

    if (task == null)
    {
        return Results.NotFound(new { Error = "Задача не найдена" });       // 404
    }

    return Results.Ok(task);                                                // 200
});

// --- POST /api/tasks - добавить (зан1 зад9) ---
// шлю { "title": "Купить хлеб" } -> 201 и созданная задача
tasksApi.MapPost("/", (CreateTaskRequest request) =>
{
    // IsNullOrWhiteSpace - true если null, "" или одни пробелы
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { Error = "Название задачи не может быть пустым" }); // 400
    }

    TaskItem created = TaskStorage.Add(request.Title.Trim());   // Trim - убрать пробелы по краям

    // 201 Created - создано, в ответе сама задача уже с Id
    return Results.Json(created, statusCode: StatusCodes.Status201Created);
});

// --- DELETE /api/tasks/{id} (зан1 зад10) ---
tasksApi.MapDelete("/{id:int}", (int id) =>
{
    bool deleted = TaskStorage.Delete(id);

    if (!deleted)
    {
        return Results.NotFound(new { Error = "Задача не найдена" });       // 404
    }

    return Results.Ok(new { Message = $"Задача №{id} удалена" });           // 200
});

// --- PUT /api/tasks/{id} - галочка выполнено (дз зан1) ---
// шлю { "isDone": true } -> 200 и задача
tasksApi.MapPut("/{id:int}", (int id, UpdateTaskStatusRequest request) =>
{
    TaskItem? task = TaskStorage.GetById(id);

    if (task == null)
    {
        return Results.NotFound(new { Error = "Задача не найдена" });       // 404
    }

    task.IsDone = request.IsDone;      // меняю объект прям в списке
    return Results.Ok(task);                                                // 200
});

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
public record CreateTaskRequest(string? Title);

// дз: тело запроса PUT /api/tasks/{id}
public record UpdateTaskStatusRequest(bool IsDone);
