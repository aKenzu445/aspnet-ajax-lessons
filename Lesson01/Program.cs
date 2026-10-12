// занятие 1. веб-сервер на asp.net core и первые ajax запросы
using System.Globalization;

// builder - тут настраиваю сервисы и конфиг
var builder = WebApplication.CreateBuilder(args);

// app - само приложение (внутри сервак Kestrel)
var app = builder.Build();

// === зад4: статические файлы из wwwroot ===
// UseDefaultFiles - на "/" отдает index.html
// UseStaticFiles - отдает файлы из wwwroot как есть (html, css, js)
// без него /index.html был бы 404, сервак про wwwroot не знал бы
app.UseDefaultFiles();
app.UseStaticFiles();

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

// запуск сервака: начинает слушать порт и обрабатывать запросы
app.Run();


// === классы (типы данных) ===
// в файле с top-level кодом классы пишутся ПОСЛЕ основного кода

// зад6: тело POST /api/echo. string? - строка которая может быть null, record - короткий класс для данных
public record EchoRequest(string? Text);
