using Marten;

namespace API.Infrastructure.Ingestion.DeadLetters;

public static class Endpoint
{
    private const int MaxTake = 500;

    public static IEndpointRouteBuilder MapDeadLetterEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dead-letters", async (
            IQuerySession session,
            CancellationToken cancellationToken,
            DeadLetterReason? reason = null,
            int take = 100) =>
        {
            take = Math.Clamp(take, 1, MaxTake);

            var query = session.Query<DeadLetterEvent>().AsQueryable();
            if (reason is not null)
            {
                query = query.Where(x => x.Reason == reason);
            }

            var items = await query
                .OrderByDescending(x => x.DeadLetteredAt)
                .Take(take)
                .ToListAsync(cancellationToken);

            return Results.Ok(items);
        })
        .WithName("GetDeadLetters")
        .WithTags("DeadLetters")
        .Produces<IReadOnlyList<DeadLetterEvent>>();

        app.MapGet("/api/dead-letters/{id:guid}", async (
            Guid id,
            IQuerySession session,
            CancellationToken cancellationToken) =>
        {
            var item = await session.LoadAsync<DeadLetterEvent>(id, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetDeadLetter")
        .WithTags("DeadLetters")
        .Produces<DeadLetterEvent>()
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}