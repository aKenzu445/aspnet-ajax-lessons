namespace Lesson1.WebServer.Middleware;

// === дз зан2: ограничение частоты запросов (rate limiting) ===
// если с одного ip больше MaxRequests запросов за WindowSeconds сек -> 429
// (цифры в appsettings.json, секция RateLimiting)
// храню в памяти: словарь ip -> список времени запросов, без всяких библиотек
// считаю тока /api/*, статика отдается раньше, а то одна загрузка страницы съела бы весь лимит
public class RateLimitingMiddleware
{
    // зан3: лимиты беру из appsettings.json (RateLimiting), а не хардкод. если нету - 5 за 10 сек
    private readonly int MaxRequests;
    private readonly int WindowSeconds;

    // static - один словарь на все запросы. ключ - ip, значение - когда были его запросы
    private static readonly Dictionary<string, List<DateTime>> RequestsByIp = new();

    // "замок": запросы идут параллельно, а Dictionary и List не умеют когда их меняют сразу из нескольких потоков
    private static readonly object Lock = new();

    private readonly RequestDelegate _next;

    // IConfiguration - доступ к appsettings.json, asp.net сам кидает его в конструктор (DI)
    public RateLimitingMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        MaxRequests = configuration.GetValue("RateLimiting:MaxRequests", 5);
        WindowSeconds = configuration.GetValue("RateLimiting:WindowSeconds", 10);
    }

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
