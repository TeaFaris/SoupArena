namespace SoupArena.DataBase.Credentials
{
    public interface IDBCredentials
    {
        public string Host { get; init; }
        public uint Port { get; init; }
        public string Password { get; init; }
        public string Username { get; init; }
    }
}
