using Microsoft.EntityFrameworkCore;
using MinimalAPIsMovies.DTOs;
using MinimalAPIsMovies.Entities;
using MinimalAPIsMovies.Repositories;
using MinimalAPIsMovies;

public class ActorsRepository : IActorsRepository
{
    private readonly ApplicationDbContext context;
    private readonly IHttpContextAccessor httpContextAccessor;

    public ActorsRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        this.context = context;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<Actor>> GetAll(PaginationDTO pagination)
    {
        var queryable = context.Actors.AsQueryable();
        await httpContextAccessor
            .HttpContext!.InsertPaginationParameterInResponseHeader(queryable);
        return await queryable.OrderBy(a => a.Name).Paginate(pagination).ToListAsync();
    }

    public async Task<Actor?> GetById(int id)
    {
        return await context.Actors.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Actor>> GetByName(string name)
    {
        return await context.Actors
            .Where(a => a.Name.Contains(name))
            .OrderBy(a => a.Name).ToListAsync();
    }

    public async Task<int> Create(Actor actor)
    {
        context.Add(actor);
        await context.SaveChangesAsync();
        return actor.Id;
    }

    public async Task<bool> Exist(int id)
    {
        return await context.Actors.AnyAsync(a => a.Id == id);
    }

    public async Task Update(Actor actor)
    {
        context.Update(actor);
        await context.SaveChangesAsync();
    }

    public async Task Delete(int id)
    {
        await context.Actors.Where(a => a.Id == id).ExecuteDeleteAsync();
    }

    // Add this Detach method
    public void Detach(Actor actor)
    {
        context.Entry(actor).State = EntityState.Detached;
    }
}
