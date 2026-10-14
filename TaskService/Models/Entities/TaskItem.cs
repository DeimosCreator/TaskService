using TaskApi.Models.Entities.Enums;

namespace TaskApi.Models.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public Priority Priority { get; set; }
    public uint Version { get; set; }
}