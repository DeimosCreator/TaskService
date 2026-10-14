using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Models.Dtos;
using TaskApi.Models.Entities;
using TaskApi.Services.Interfaces;

namespace TaskApi.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tasks");

        group.MapPost("/", async (CreateTaskItemDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length > 200)
            {
                return Results.BadRequest(new { error = "Title must not be empty and up to 200 characters." });
            }

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = dto.Title.Trim(),
                Priority = dto.Priority
            };

            db.TaskItems.Add(task);
            await db.SaveChangesAsync();

            var resultDto = new TaskItemDto(
                task.Id, task.Title, task.IsCompleted, task.CreatedAt, task.CompletedAt, task.Priority);

            return Results.Created($"/tasks/{task.Id}", resultDto);
        });

        group.MapGet("/", async (AppDbContext db) =>
        {
            var tasks = await db.TaskItems
                .AsNoTracking()
                .Select(t => new TaskItemDto(t.Id, t.Title, t.IsCompleted, t.CreatedAt, t.CompletedAt, t.Priority))
                .ToListAsync();

            return Results.Ok(tasks);
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var task = await db.TaskItems.FindAsync(id);
            if (task is null) return Results.NotFound();

            db.TaskItems.Remove(task);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        group.MapPut("/{id:guid}/complete", async (
            Guid id, 
            AppDbContext db, 
            ITaskEventPublisher publisher, 
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("CompleteTaskEndpoint");

            var task = await db.TaskItems.FindAsync(id);
            if (task is null) return Results.NotFound();

            if (task.IsCompleted)
            {
                return Results.Conflict(new { error = "Task is already completed." });
            }

            task.IsCompleted = true;
            task.CompletedAt = DateTimeOffset.UtcNow;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { error = "Conflict: Task was modified by another request." });
            }

            try
            {
                var @event = new TaskCompletedEvent
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    CompletedAt = task.CompletedAt,
                    Priority = task.Priority
                };

                await publisher.PublishTaskCompletedAsync(@event);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish TaskCompletedEvent to RabbitMQ for TaskId {TaskId}", task.Id);
            }

            return Results.Ok(new TaskItemDto(
                task.Id, task.Title, task.IsCompleted, task.CreatedAt, task.CompletedAt, task.Priority));
        });

        return app;
    }
}
