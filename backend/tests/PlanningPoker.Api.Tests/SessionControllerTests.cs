using Microsoft.AspNetCore.SignalR;
using Moq;
using PlanningPoker.Api.Controllers;
using PlanningPoker.Api.Data;
using PlanningPoker.Api.Hubs;
using PlanningPoker.Api.Services;

namespace PlanningPoker.Api.Tests;

public class SessionControllerTests
{

    private readonly SessionsController _controller;
    private Mock<IPinGenerator> _pinGenerator;
    private Mock<IHubContext<PlanningPokerHub>> _hubContext;
    private Mock<PlanningPokerDbContext> _dbContext;
    
    public SessionControllerTests()
    {
        _pinGenerator = new Mock<IPinGenerator>();
        _hubContext = new Mock<IHubContext<PlanningPokerHub>>();
        _dbContext = new Mock<PlanningPokerDbContext>();
        _controller = new SessionsController(_dbContext.Object, _pinGenerator.Object, _hubContext.Object);
    }

    [Fact]
    public void Test1()
    {
        Assert.True(true);
    }
}