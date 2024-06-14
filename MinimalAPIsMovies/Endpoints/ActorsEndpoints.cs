using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using MinimalAPIsMovies.DTOs;
using MinimalAPIsMovies.Entities;
using MinimalAPIsMovies.Repositories;
using MinimalAPIsMovies.Services;
using System.Collections.Generic;

namespace MinimalAPIsMovies.Endpoints
{
    public static class ActorsEndpoints
    {
        private readonly static string container = "actors";
        public static RouteGroupBuilder MapActors(this RouteGroupBuilder group)
        {
            group.MapGet("/", GetAll).
                CacheOutput(c => c.Expire(TimeSpan.FromMinutes(1)).Tag("actors-get"));
            group.MapGet("getByName/{name}", GetByName);
            group.MapGet("/{id:int}", GetById);
            group.MapPost("/", Create).DisableAntiforgery();
            group.MapPut("/{id:int}", Update).DisableAntiforgery();
            group.MapDelete("/{id:int}", Delete);
            return group;
        }

        static async Task<Ok<List<ActorDTO>>> GetAll(IActorsRepository repository, 
            IMapper mapper, int page = 1, int recordsPerPage = 10)
        {
            var pagination = new PaginationDTO { Page = page, RecordsPerPage = recordsPerPage };
            var actors = await repository.GetAll(pagination);
            var actorDTOs = mapper.Map<List<ActorDTO>>(actors);
            return TypedResults.Ok(actorDTOs);
        }

        static async Task<Ok<List<ActorDTO>>> GetByName(string name, IActorsRepository repository, IMapper mapper)
        {
            var actors = await repository.GetByName(name);
            var actorDTOs = mapper.Map<List<ActorDTO>>(actors);
            return TypedResults.Ok(actorDTOs);
        }

        static async Task<Results<Ok<ActorDTO>, NotFound>> GetById(int id,
            IActorsRepository repository, IMapper mapper)
        {
            var actor = await repository.GetById(id);

            if (actor is null)
            {
                return TypedResults.NotFound();
            }

            var actorDTO = mapper.Map<ActorDTO>(actor);
            return TypedResults.Ok(actorDTO);
        }

        static async Task<Created<ActorDTO>> Create([FromForm] CreateActorDTO createActorDTO,
            IActorsRepository repository, IOutputCacheStore outputCacheStore,
            IMapper mapper, IFileStorage fileStorage)
        {
            var actor = mapper.Map<Actor>(createActorDTO);

            if (createActorDTO.Picture is not null)
            {
                var url = await fileStorage.Store(container, createActorDTO.Picture);
                actor.Picture = url;
            }

            var id = await repository.Create(actor);
            await outputCacheStore.EvictByTagAsync("actors-get", default);
            var actorDTO = mapper.Map<ActorDTO>(actor);
            return TypedResults.Created($"/actors/{id}", actorDTO);
        }

        static async Task<Results<NoContent, NotFound>> Update(int id,
            [FromForm] CreateActorDTO createActorDTO, IActorsRepository repository,
            IFileStorage fileStorage, IOutputCacheStore outputCacheStore,
            IMapper mapper)
        {
            var actorDB = await repository.GetById(id);

            if (actorDB is null)
            {
                return TypedResults.NotFound();
            }

            // Map the DTO to a new instance of the entity
            var actorForUpdate = mapper.Map<Actor>(createActorDTO);
            actorForUpdate.Id = id;
            actorForUpdate.Picture = actorDB.Picture;

            if (createActorDTO.Picture is not null)
            {
                var url = await fileStorage.Edit(actorForUpdate.Picture,
                    container, createActorDTO.Picture);
                actorForUpdate.Picture = url;
            }

            // Detach the existing tracked entity instance
            repository.Detach(actorDB);

            await repository.Update(actorForUpdate);
            await outputCacheStore.EvictByTagAsync("actors-get", default);
            return TypedResults.NoContent();
        }

        public static async Task<Results<NoContent, NotFound>> Delete(int id, 
            IActorsRepository repository, IOutputCacheStore outputCacheStore, 
            IFileStorage fileStorage)
        {
            var actorDB = await repository.GetById(id);

            if (actorDB is null)
            {
                return TypedResults.NotFound();
            }

            // Delete the actor from the database
            await repository.Delete(id);

            // Delete the associated picture if it exists
            if (!string.IsNullOrEmpty(actorDB.Picture))
            {
                Console.WriteLine($"Attempting to delete file: {actorDB.Picture}");
                await fileStorage.Delete(actorDB.Picture, container);
            }

            // Evict the cache
            await outputCacheStore.EvictByTagAsync("actors-get", default);
            return TypedResults.NoContent();
        }

    }
}
