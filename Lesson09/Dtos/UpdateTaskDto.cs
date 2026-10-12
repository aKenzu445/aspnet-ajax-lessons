namespace Lesson1.WebServer.Dtos;

// === зан9 зад4: DTO для ОБНОВЛЕНИЯ (PUT /api/tasks/{id}) ===
// обновляю всё: название, статус, категорию. Id берется из адреса, CreatedAt менять нельзя - их тут нет
public record UpdateTaskDto(string? Title, bool IsDone, int? CategoryId);
