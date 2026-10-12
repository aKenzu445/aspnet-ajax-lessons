// занятие 1. веб-сервер на asp.net core и первые ajax запросы

// builder - тут настраиваю сервисы и конфиг
var builder = WebApplication.CreateBuilder(args);

// app - само приложение (внутри сервак Kestrel)
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

// === зад2: первый свой endpoint ===
// GET /api/hello -> просто текст
app.MapGet("/api/hello", () => "Привет! Сервер работает.");

// запуск сервака: начинает слушать порт и обрабатывать запросы
app.Run();
