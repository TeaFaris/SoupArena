using SoupArena.DataBase.Credentials;

namespace SoupArena.DataBase
{
    public interface IDBContext<TCredentials> where TCredentials : IDBCredentials
    {
        protected TCredentials Credits { get; init; }
    }
}
