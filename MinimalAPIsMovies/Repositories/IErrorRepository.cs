using MinimalAPIsMovies.Entities;

namespace MinimalAPIsMovies.Repositories
{
    public interface IErrorRepository
    {
        Task Create(Error error);
    }
}