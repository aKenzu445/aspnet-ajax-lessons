using Lesson1.WebServer.Api;

namespace Lesson1.WebServer.Middleware;

// === зан2 зад7: простая авторизация по api ключу ===
// middleware отдельным классом (способ 2 из теории)
// менять задачи (POST, PUT, DELETE на /api/tasks) можно тока с заголовком X-Api-Key, GET открыт всем
//
// почему middleware, а не MapGroup + AddEndpointFilter: в задании просят класс по способу 2,
// и он проверяет все сразу до маршрутизации, в одном месте
// PUT тоже проверяю, хоть в задании тока POST и DELETE: он из дз зан1 и тоже меняет данные
public class ApiKeyMiddleware
{
    private const string HeaderName = "X-Api-Key";
    private const string SecretKey = "lesson2-secret";

    // _next - следующий middleware. readonly - задается один раз в конструкторе
    private readonly RequestDelegate _next;

    // конструктор: next asp.net сам сюда кидает при UseMiddleware<...>()
    public ApiKeyMiddleware(RequestDelegate next) => _next = next;

    // вызывается на КАЖДЫЙ запрос
    public async Task InvokeAsync(HttpContext context)
    {
        string method = context.Request.Method;

        bool isTasksApi = context.Request.Path.StartsWithSegments("/api/tasks");
        bool isModifying = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method);

        if (isTasksApi && isModifying)
        {
            // значение заголовка или пусто если его нет
            string? key = context.Request.Headers[HeaderName];

            if (key != SecretKey)
            {
                // 401 в общем формате (зан9 зад6)
                await ApiError.WriteAsync(context, StatusCodes.Status401Unauthorized,
                    "Неверный или отсутствующий API-ключ", ApiError.Unauthorized);
                return;   // next() НЕ зову - запрос дальше не идет, endpoint не выполнится
            }
        }

        await _next(context);   // ключ норм (или проверка не нужна) - пускаю дальше
    }
}
