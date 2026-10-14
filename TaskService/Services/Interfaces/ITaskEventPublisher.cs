using TaskApi.Models.Dtos;

namespace TaskApi.Services.Interfaces;

public interface ITaskEventPublisher
{
    Task PublishTaskCompletedAsync(TaskCompletedEvent @event, CancellationToken cancellationToken = default);
}