namespace Lesson1.WebServer.Dtos;

// === зан9 зад5: DTO категории ===
// вместо списка задач (опять был бы цикл Category -> Tasks -> Category ...) отдаю тока ЧИСЛО задач - TaskCount
public record CategoryDto(int Id, string Name, int TaskCount);
