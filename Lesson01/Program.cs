// занятие 1. веб-сервер на asp.net core и первые ajax запросы

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

// запуск сервака: начинает слушать порт и обрабатывать запросы
app.Run();
