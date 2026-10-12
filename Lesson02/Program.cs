// занятие 2. маршрутизация и middleware (продолжаю проект с зан1)
using System.Diagnostics;          // Stopwatch - секундомер (зан2 зад3)
using System.Globalization;
using Lesson1.WebServer.Data;     // TaskStorage - хранилище задач (папка Data)
using Lesson1.WebServer.Middleware; // ApiKeyMiddleware (папка Middleware)
using Lesson1.WebServer.Models;   // TaskItem - класс задачи (папка Models)

// builder - тут настраиваю сервисы и конфиг
var builder = WebApplication.CreateBuilder(args);

// app - само приложение (внутри сервак Kestrel)
var app = builder.Build();

// === зан2 зад8: глобальная обработка ошибок ===
// стоит ПЕРВЫМ и оборачивает всё в try/catch
// если где-то дальше вылетит исключение - клиент получит норм json 500, а не страницу с кучей стека
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
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

// === зан1 зад4: статические файлы из wwwroot ===
// UseDefaultFiles - на "/" отдает index.html
// UseStaticFiles - отдает файлы из wwwroot как есть (html, css, js)
// без него /index.html был бы 404, сервак про wwwroot не знал бы
app.UseDefaultFiles();
app.UseStaticFiles();

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
    ServerName = "Lesson1.WebServer",
    Version = "1.0",
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

// === зад8: список задач из памяти сервака (GET) ===
// GET /api/tasks -> массив задач в json
app.MapGet("/api/tasks", () => TaskStorage.GetAll());

// ===== Занятие 2, задача 4. Одна задача по id (параметр маршрута) =====
// GET /api/tasks/1 → 200 + задача; GET /api/tasks/99 → 404
// Задача 5: {id:int} — ограничение маршрута (route constraint). Маршрут подходит,
// ТОЛЬКО если id — целое число. /api/tasks/abc → 404 ещё до запуска нашего кода.
// (Без :int запрос /api/tasks/abc давал 400 с исключением BadHttpRequestException.)
// Это же ограничение добавлено к DELETE и PUT ниже.
app.MapGet("/api/tasks/{id:int}", (int id) =>
{
    TaskItem? task = TaskStorage.GetById(id);

    if (task == null)
    {
        return Results.NotFound(new { Error = "Задача не найдена" });       // 404
    }

    return Results.Ok(task);                                                // 200
});

// === зад9: добавить задачу (POST) ===
// POST /api/tasks с { "title": "Купить хлеб" } -> 201 и созданная задача
app.MapPost("/api/tasks", (CreateTaskRequest request) =>
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

// === зад10: удаление (DELETE) ===
// /api/tasks/2 - {id} это часть адреса, не query string. "2" само превращается в int
app.MapDelete("/api/tasks/{id:int}", (int id) =>
{
    bool deleted = TaskStorage.Delete(id);

    if (!deleted)
    {
        return Results.NotFound(new { Error = "Задача не найдена" });       // 404
    }

    return Results.Ok(new { Message = $"Задача №{id} удалена" });           // 200
});

// === дз: отметить задачу выполненной (PUT) ===
// PUT /api/tasks/2 с { "isDone": true } -> 200 и задача
// PUT это когда обновляешь то что уже есть
app.MapPut("/api/tasks/{id:int}", (int id, UpdateTaskStatusRequest request) =>
{
    TaskItem? task = TaskStorage.GetById(id);

    if (task == null)
    {
        return Results.NotFound(new { Error = "Задача не найдена" });       // 404
    }

    task.IsDone = request.IsDone;      // меняю объект прям в списке
    return Results.Ok(task);                                                // 200
});

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
