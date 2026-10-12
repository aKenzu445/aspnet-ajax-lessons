namespace Lesson1.WebServer.Data;

// дз зан3: инфа о серваке в одном месте, юзают и /api/server-info и страница /About
public static class ServerInfo
{
    // const - константа, не меняется
    public const string Name = "Lesson1.WebServer";
    public const string Version = "1.0";

    // когда запустился сервак. пишется один раз в Program.cs на старте
    public static DateTime StartedAt { get; set; }
}
