namespace Lesson1.WebServer.Dtos;

// === зан9 зад3: DTO для СОЗДАНИЯ задачи (что приходит в систему) ===
// защита от overposting: тут физически НЕТ Id, IsDone, CreatedAt
// даже если пришлют { "id": 999, "isDone": true } - записать некуда
// сервак решает сам: Id дает база, IsDone = false, CreatedAt = DateTime.Now. DueDate - срок из дз зан7
public record CreateTaskDto(string? Title, int? CategoryId = null, DateTime? DueDate = null);
