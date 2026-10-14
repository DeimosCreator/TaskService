using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TaskApi.Models.Dtos;
using TaskApi.Models.Entities.Enums;
using TaskApi.Services.Interfaces;

namespace TaskService.Tests;

public class CompleteTaskIntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CompleteTask_ShouldUpdateStatus_AndPublishEventToRabbitMq()
    {
        var mockPublisher = new Mock<ITaskEventPublisher>();

        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres");
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITaskEventPublisher));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton(mockPublisher.Object);
            });
        }).CreateClient();

        var createRequest = new CreateTaskItemDto("Buy milk", Priority.High);
        var postResponse = await client.PostAsJsonAsync("/tasks", createRequest);

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var createdTask = await postResponse.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(createdTask);
        Assert.False(createdTask.IsCompleted);
        Assert.Null(createdTask.CompletedAt);

        var putResponse = await client.PutAsync($"/tasks/{createdTask.Id}/complete", null);

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        mockPublisher.Verify(
            publisher => publisher.PublishTaskCompletedAsync(
                It.Is<TaskCompletedEvent>(e => 
                    e.TaskId == createdTask.Id && 
                    e.Title == "Buy milk" &&
                    e.Priority == Priority.High),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
