using MinimalAPIsMovies.Entities;

namespace MinimalAPIsMovies.Repositories
{
    public class ErrorRepository(ApplicationDbContext context) : IErrorRepository
    {
        public async Task Create(Error error)
        {
            context.Add(error);
            await context.SaveChangesAsync();
        }
    }
}
