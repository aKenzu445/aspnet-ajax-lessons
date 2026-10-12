namespace Lesson1.WebServer.Dtos;

// === зан9 зад8: "конверт" для ответа по страницам ===
// кроме Items клиент получает инфу о страницах: какая, сколько на странице, сколько всего, сколько страниц
// <T> - generic, такой конверт подойдет и для задач и для категорий
public record PagedResultDto<T>(List<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
