using System.Text.Json.Serialization;
using TaskApi.Models.Entities.Enums;

namespace TaskApi.Models.Dtos;

public record TaskItemDto(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    Priority Priority);
    
public record CreateTaskItemDto(
    string Title,
    Priority Priority = Priority.Medium);
    
public record TaskCompletedEvent
{
    [JsonPropertyName("taskId")]
    public Guid TaskId { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("completedAt")]
    public DateTimeOffset CompletedAt { get; init; }

    [JsonPropertyName("priority")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Priority Priority { get; init; }
}