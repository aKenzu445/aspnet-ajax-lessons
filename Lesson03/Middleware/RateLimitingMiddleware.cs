namespace Lesson1.WebServer.Middleware;

// === дз зан2: ограничение частоты запросов (rate limiting) ===
// если с одного ip больше MaxRequests запросов за WindowSeconds сек -> 429
// храню в памяти: словарь ip -> список времени запросов, без всяких библиотек
// считаю тока /api/*, статика отдается раньше, а то одна загрузка страницы съела бы весь лимит
public class RateLimitingMiddleware
{
    private const int MaxRequests = 5;        // не больше 5 запросов...
    private const int WindowSeconds = 10;     // ...за 10 сек

    // static - один словарь на все запросы. ключ - ip, значение - когда были его запросы
    private static readonly Dictionary<string, List<DateTime>> RequestsByIp = new();

    // "замок": запросы идут параллельно, а Dictionary и List не умеют когда их меняют сразу из нескольких потоков
    private static readonly object Lock = new();

    private readonly RequestDelegate _next;

    public RateLimitingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // ip клиента, если вдруг нету - "unknown"
        string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        DateTime now = DateTime.UtcNow;
        bool tooMany;

        lock (Lock)   // внутри lock код выполняет тока один поток за раз
        {
            // ip первый раз - создаю ему пустой список
            if (!RequestsByIp.TryGetValue(ip, out List<DateTime>? times))
            {
                times = new List<DateTime>();
                RequestsByIp[ip] = times;
            }

            // выкидываю записи старше 10 сек - они уже не в счет
            times.RemoveAll(t => (now - t).TotalSeconds > WindowSeconds);

            tooMany = times.Count >= MaxRequests;
            if (!tooMany)
            {
                times.Add(now);   // запоминаю этот запрос
            }
        }

        if (tooMany)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;   // 429
            await context.Response.WriteAsJsonAsync(new
            {
                error = $"Слишком много запросов: не больше {MaxRequests} за {WindowSeconds} секунд. Подождите немного."
            });
            return;
        }

        await _next(context);
    }
}
