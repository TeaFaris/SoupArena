namespace SoupArena.DataBase.Credentials
{
    public readonly struct PostgresCredentials : IDBCredentials
    {
        public PostgresCredentials() { }
        public required string DataBase { get; init; }
        public string Host { get; init; } = "localhost";
        public uint Port { get; init; } = 5432;
        public required string Password { get; init; }
        public string Username { get; init; } = "postgres";
    }
}
