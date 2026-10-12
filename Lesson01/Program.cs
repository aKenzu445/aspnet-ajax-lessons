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

// запуск сервака: начинает слушать порт и обрабатывать запросы
app.Run();
