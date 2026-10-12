namespace Lesson1.WebServer.Api;

// === зан9 зад6: один формат ошибок для всего api ===
// у ВСЕХ ошибок одна структура: { "error": { "message": "Задача не найдена", "code": "TASK_NOT_FOUND" } }
// message - для человека, code - для программы (js, мобилка)
// клиенту хватит одного кода обработки ошибок на любой endpoint
public static class ApiError
{
    // коды ошибок для программы (const - не меняются)
    public const string ValidationError = "VALIDATION_ERROR";
    public const string TaskNotFound = "TASK_NOT_FOUND";
    public const string InvalidParameters = "INVALID_PARAMETERS";
    public const string DivisionByZero = "DIVISION_BY_ZERO";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string TooManyRequests = "TOO_MANY_REQUESTS";
    public const string Maintenance = "MAINTENANCE";
    public const string RouteNotFound = "ROUTE_NOT_FOUND";
    public const string InternalError = "INTERNAL_ERROR";

    // тело ответа: анонимный объект -> { "error": { "message", "code" } }
    public static object Body(string message, string code) => new { error = new { message, code } };

    // готовый результат для endpoints: Results.Json с нужным кодом
    public static IResult Result(int statusCode, string message, string code) =>
        Results.Json(Body(message, code), statusCode: statusCode);

    public static IResult BadRequest(string message, string code) => Result(StatusCodes.Status400BadRequest, message, code);

    public static IResult NotFound(string message, string code) => Result(StatusCodes.Status404NotFound, message, code);

    // ошибки валидации: склеиваю в одно через "; "
    public static IResult Validation(IEnumerable<string?> messages) =>
        BadRequest(string.Join("; ", messages), ValidationError);

    // для middleware (там нет IResult, пишу ответ прям в HttpContext)
    public static Task WriteAsync(HttpContext context, int statusCode, string message, string code)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(Body(message, code));
    }
}
