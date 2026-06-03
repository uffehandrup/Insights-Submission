using System.Reflection;

namespace API.Domains;

public static class EventTypeRegistry
{
    public static readonly IReadOnlyDictionary<string, Type> Map = Build();

    private static IReadOnlyDictionary<string, Type> Build()
    {
        var pairs = typeof(DomainEvent).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(DomainEvent).IsAssignableFrom(t))
            .Select(t => (Type: t, Attr: t.GetCustomAttribute<EventTypeAttribute>()))
            .Where(x => x.Attr is not null)
            .ToList();

        var missing = typeof(DomainEvent).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(DomainEvent).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttribute<EventTypeAttribute>() is null)
            .Select(t => t.FullName)
            .ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Concrete DomainEvent types missing [EventType] attribute: {string.Join(", ", missing)}");

        var duplicates = pairs
            .GroupBy(x => x.Attr!.Discriminator)
            .Where(g => g.Count() > 1)
            .Select(g => $"'{g.Key}' on [{string.Join(", ", g.Select(x => x.Type.FullName))}]")
            .ToList();

        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                $"Duplicate [EventType] discriminators: {string.Join("; ", duplicates)}");

        return pairs.ToDictionary(x => x.Attr!.Discriminator, x => x.Type);
    }
}
