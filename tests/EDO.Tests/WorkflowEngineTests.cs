using EDO.Server.Data;
using EDO.Server.Models;
using EDO.Server.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EDO.Tests;

public class WorkflowEngineTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly WorkflowEngineService _engine;

    public WorkflowEngineTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _engine = new WorkflowEngineService(_db);

        SeedData();
    }

    private void SeedData()
    {
        var roleInitiator = new Role { Id = 1, Name = "Инициатор" };
        var roleManager = new Role { Id = 2, Name = "Руководитель" };
        var roleDirector = new Role { Id = 3, Name = "Директор" };

        _db.Roles.AddRange(roleInitiator, roleManager, roleDirector);

        _db.ApprovalStages.AddRange(
            new ApprovalStage { Id = 1, Name = "Согласование руководителем", RoleId = 2, OrderSequence = 1 },
            new ApprovalStage { Id = 2, Name = "Утверждение директором", RoleId = 3, OrderSequence = 2 }
        );

        _db.Users.Add(new User
        {
            Id = 1, FirstName = "Иван", LastName = "Иванов",
            Position = "Руководитель", Email = "manager@test.com",
            PasswordHash = "hash", RoleId = 2
        });

        _db.SaveChanges();
    }

    [Fact]
    public async Task ProcessDecision_Approved_MovesToNextStage()
    {
        // Arrange
        var request = new WorkflowDecisionRequest
        {
            DocumentId = 100,
            UserId = 1,
            CurrentStageId = 1,
            Decision = Decision.Approved,
            Comment = "Согласовано"
        };

        // Act
        var result = await _engine.ProcessDecisionAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.NextStageId.Should().Be(2);
        result.IsCompleted.Should().BeFalse();
        result.IsRejected.Should().BeFalse();

        var history = await _db.ActionHistories
            .FirstOrDefaultAsync(h => h.DocumentId == 100);
        Assert.NotNull(history);
        Assert.Equal(Decision.Approved, history!.Decision);
        Assert.Equal("Согласовано", history.Comment);
    }

    [Fact]
    public async Task ProcessDecision_Approved_OnLastStage_CompletesWorkflow()
    {
        // Arrange — решение на последнем этапе
        var request = new WorkflowDecisionRequest
        {
            DocumentId = 200,
            UserId = 1,
            CurrentStageId = 2,
            Decision = Decision.Approved,
            Comment = "Утверждаю"
        };

        // Act
        var result = await _engine.ProcessDecisionAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.NextStageId.Should().BeNull();
        result.IsCompleted.Should().BeTrue();
        result.IsRejected.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessDecision_Rejected_ReturnsToInitiator()
    {
        // Arrange
        var request = new WorkflowDecisionRequest
        {
            DocumentId = 300,
            UserId = 1,
            CurrentStageId = 1,
            Decision = Decision.Rejected,
            Comment = "Отклонено — недостаточно обоснований"
        };

        // Act
        var result = await _engine.ProcessDecisionAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.IsRejected.Should().BeTrue();
        result.IsCompleted.Should().BeFalse();
        result.NextStageId.Should().BeNull();

        var history = await _db.ActionHistories
            .FirstOrDefaultAsync(h => h.DocumentId == 300);
        Assert.NotNull(history);
        Assert.Equal(Decision.Rejected, history!.Decision);
    }

    [Fact]
    public async Task ProcessDecision_InvalidStage_ReturnsFailure()
    {
        // Arrange — несуществующий этап
        var request = new WorkflowDecisionRequest
        {
            DocumentId = 400,
            UserId = 1,
            CurrentStageId = 999,
            Decision = Decision.Approved
        };

        // Act
        var result = await _engine.ProcessDecisionAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("не найден");
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
