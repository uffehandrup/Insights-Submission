using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Domains;
using API.Domains.Workflows;

namespace Tests;

// FR-002: Event-type registry & deserialization.
// The dispatcher discovers concrete DomainEvent types by reflection, keyed on
// the [EventType(…)] discriminator. These tests pin that contract so
// e.g., a duplicated discriminator fails fast.
public class EventTypeRegistryTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static IEnumerable<Type> ConcreteEventTypes =>
        typeof(DomainEvent).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(DomainEvent).IsAssignableFrom(t));

    [Fact]
    public void Every_concrete_event_declares_an_EventType_attribute()
    {
        var missing = ConcreteEventTypes
            .Where(t => t.GetCustomAttribute<EventTypeAttribute>() is null)
            .Select(t => t.FullName)
            .ToList();

        Assert.True(missing.Count == 0,
            $"Concrete DomainEvent types missing [EventType]: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Discriminators_are_unique()
    {
        var duplicates = ConcreteEventTypes
            .Select(t => t.GetCustomAttribute<EventTypeAttribute>()!.Discriminator)
            .GroupBy(d => d)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate discriminators: {string.Join(", ", duplicates)}");
    }

    [Theory]
    [InlineData("workflow.started",        typeof(WorkflowStartedDomainEvent))]
    [InlineData("workflow.step.completed", typeof(WorkflowStepCompletedDomainEvent))]
    [InlineData("workflow.completed",      typeof(WorkflowCompletedDomainEvent))]
    [InlineData("workflow.failed",         typeof(WorkflowFailedDomainEvent))]
    [InlineData("workflow.parked",         typeof(WorkflowParkedDomainEvent))]
    [InlineData("workflow.resumed",        typeof(WorkflowResumedDomainEvent))]
    [InlineData("workflow.cancelled",      typeof(WorkflowCancelledDomainEvent))]
    public void Discriminator_resolves_to_expected_concrete_type(string discriminator, Type expectedType)
    {
        var resolved = ConcreteEventTypes
            .Single(t => t.GetCustomAttribute<EventTypeAttribute>()!.Discriminator == discriminator);

        Assert.Equal(expectedType, resolved);
    }

    [Fact]
    public void Payload_deserializes_into_concrete_type_with_populated_fields()
    {
        var payload = """
        {
            "eventType":    "workflow.started",
            "eventId":      "11111111-1111-1111-1111-111111111111",
            "streamId":     "stream-1",
            "workflowId":   42,
            "workflowName": "Order Processing",
            "occurredAt":   "2026-05-20T10:00:00Z"
        }
        """;

        var result = (WorkflowStartedDomainEvent?)JsonSerializer.Deserialize(payload, typeof(WorkflowStartedDomainEvent), Options);

        Assert.NotNull(result);
        Assert.Equal("11111111-1111-1111-1111-111111111111", result!.EventId);
        Assert.Equal("stream-1", result.StreamId);
        Assert.Equal(42, result.WorkflowId);
        Assert.Equal("Order Processing", result.WorkflowName);
    }
}
