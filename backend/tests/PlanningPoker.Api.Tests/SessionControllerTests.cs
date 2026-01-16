using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using PlanningPoker.Api.Controllers;
using PlanningPoker.Api.Data;
using PlanningPoker.Api.Hubs;
using PlanningPoker.Api.Models;
using PlanningPoker.Api.Services;

namespace PlanningPoker.Api.Tests;

public class SessionControllerTests
{
    private readonly PlanningPokerDbContext _context;
    private readonly Mock<IPinGenerator> _pinGeneratorMock;
    private readonly Mock<IHubContext<PlanningPokerHub>> _hubContextMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly SessionsController _controller;

    public SessionControllerTests()
    {
        var options = new DbContextOptionsBuilder<PlanningPokerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PlanningPokerDbContext(options);

        _pinGeneratorMock = new Mock<IPinGenerator>();
        _hubContextMock = new Mock<IHubContext<PlanningPokerHub>>();
        _clientProxyMock = new Mock<IClientProxy>();
        
        var groupManagerMock = new Mock<IGroupManager>();
        _hubContextMock.Setup(h => h.Clients.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Groups).Returns(groupManagerMock.Object);

        _controller = new SessionsController(_context, _pinGeneratorMock.Object, _hubContextMock.Object);
    }

    [Fact]
    public async Task CreateSession_ValidRequest_ReturnsOkWithSessionDto()
    {
        // Arrange
        var pin = "123456";
        var sessionName = "Test Session";
        var request = new CreateSessionRequest { SessionName = sessionName };

        _pinGeneratorMock.Setup(p => p.GenerateUniquePinAsync()).ReturnsAsync(pin);

        // Act
        var result = await _controller.CreateSession(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sessionDto = Assert.IsType<SessionDto>(okResult.Value);
        
        Assert.Equal(pin, sessionDto.PIN);
        Assert.Equal(sessionName, sessionDto.SessionName);
        Assert.Equal("Active", sessionDto.Status);
        Assert.True(sessionDto.SessionId > 0);

        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.PIN == pin);
        Assert.NotNull(session);
        Assert.Equal(sessionName, session.SessionName);
    }

    [Fact]
    public async Task GetSession_ValidPin_ReturnsOkWithSessionDto()
    {
        // Arrange
        var pin = "123456";
        var session = new Session
        {
            PIN = pin,
            SessionName = "Test Session",
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.GetSession(pin);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sessionDto = Assert.IsType<SessionDto>(okResult.Value);
        
        Assert.Equal(pin, sessionDto.PIN);
        Assert.Equal("Test Session", sessionDto.SessionName);
        Assert.NotNull(sessionDto.Participants);
    }

    [Fact]
    public async Task GetSession_InvalidPin_ReturnsBadRequest()
    {
        // Arrange
        var invalidPin = "invalid";

        _pinGeneratorMock.Setup(p => p.IsValidPin(invalidPin)).Returns(false);

        // Act
        var result = await _controller.GetSession(invalidPin);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid PIN format", badRequestResult.Value);
    }

    [Fact]
    public async Task GetSession_NonExistentPin_ReturnsNotFound()
    {
        // Arrange
        var pin = "999999";

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.GetSession(pin);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Session not found", notFoundResult.Value);
    }

    [Fact]
    public async Task GetSession_WithParticipants_ReturnsParticipants()
    {
        // Arrange
        var pin = "123456";
        var session = new Session
        {
            PIN = pin,
            SessionName = "Test Session",
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var participant = new Participant
        {
            SessionId = session.SessionId,
            Name = "John Doe",
            JoinedAt = DateTime.UtcNow
        };
        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.GetSession(pin);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sessionDto = Assert.IsType<SessionDto>(okResult.Value);
        
        Assert.NotNull(sessionDto.Participants);
        Assert.Single(sessionDto.Participants);
        Assert.Equal("John Doe", sessionDto.Participants[0].Name);
    }

    [Fact]
    public async Task JoinSession_ValidRequest_ReturnsOkWithParticipantDto()
    {
        // Arrange
        var pin = "123456";
        var session = new Session
        {
            PIN = pin,
            SessionName = "Test Session",
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var request = new JoinSessionRequest { Name = "John Doe" };

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.JoinSession(pin, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var participantDto = Assert.IsType<ParticipantDto>(okResult.Value);
        
        Assert.Equal("John Doe", participantDto.Name);
        Assert.True(participantDto.ParticipantId > 0);

        _clientProxyMock.Verify(
            c => c.SendCoreAsync("ParticipantJoined", It.IsAny<object[]>(), default),
            Times.Once);

        var participant = await _context.Participants.FirstOrDefaultAsync(p => p.Name == "John Doe");
        Assert.NotNull(participant);
    }

    [Fact]
    public async Task JoinSession_InvalidPin_ReturnsBadRequest()
    {
        // Arrange
        var invalidPin = "invalid";
        var request = new JoinSessionRequest { Name = "John Doe" };

        _pinGeneratorMock.Setup(p => p.IsValidPin(invalidPin)).Returns(false);

        // Act
        var result = await _controller.JoinSession(invalidPin, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid PIN format", badRequestResult.Value);
    }

    [Fact]
    public async Task JoinSession_NonExistentSession_ReturnsNotFound()
    {
        // Arrange
        var pin = "999999";
        var request = new JoinSessionRequest { Name = "John Doe" };

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.JoinSession(pin, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Session not found", notFoundResult.Value);
    }

    [Fact]
    public async Task JoinSession_InactiveSession_ReturnsBadRequest()
    {
        // Arrange
        var pin = "123456";
        var session = new Session
        {
            PIN = pin,
            SessionName = "Test Session",
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Completed
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var request = new JoinSessionRequest { Name = "John Doe" };

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.JoinSession(pin, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Session is not active", badRequestResult.Value);
    }

    [Fact]
    public async Task SubmitVote_ValidRequest_ReturnsOk()
    {
        // Arrange
        var pin = "123456";
        var session = new Session
        {
            PIN = pin,
            SessionName = "Test Session",
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var participant = new Participant
        {
            SessionId = session.SessionId,
            Name = "John Doe",
            JoinedAt = DateTime.UtcNow
        };
        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();

        var request = new SubmitVoteRequest
        {
            ParticipantId = participant.ParticipantId,
            CardValue = "5"
        };

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.SubmitVote(pin, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        _clientProxyMock.Verify(
            c => c.SendCoreAsync("VoteSubmitted", It.IsAny<object[]>(), default),
            Times.Once);

        var vote = await _context.Votes.FirstOrDefaultAsync(v => v.ParticipantId == participant.ParticipantId);
        Assert.NotNull(vote);
        Assert.Equal("5", vote.CardValue);
    }

    [Fact]
    public async Task SubmitVote_InvalidPin_ReturnsBadRequest()
    {
        // Arrange
        var invalidPin = "invalid";
        var request = new SubmitVoteRequest
        {
            ParticipantId = 1,
            CardValue = "5"
        };

        _pinGeneratorMock.Setup(p => p.IsValidPin(invalidPin)).Returns(false);

        // Act
        var result = await _controller.SubmitVote(invalidPin, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid PIN format", badRequestResult.Value);
    }

    [Fact]
    public async Task SubmitVote_NonExistentSession_ReturnsNotFound()
    {
        // Arrange
        var pin = "999999";
        var request = new SubmitVoteRequest
        {
            ParticipantId = 1,
            CardValue = "5"
        };

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.SubmitVote(pin, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Session not found", notFoundResult.Value);
    }

    [Fact]
    public async Task SubmitVote_NonExistentParticipant_ReturnsNotFound()
    {
        // Arrange
        var pin = "123456";
        var session = new Session
        {
            PIN = pin,
            SessionName = "Test Session",
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var request = new SubmitVoteRequest
        {
            ParticipantId = 999,
            CardValue = "5"
        };

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.SubmitVote(pin, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Participant not found", notFoundResult.Value);
    }

    [Fact]
    public async Task SubmitVote_ReplacesExistingVote_ReturnsOk()
    {
        // Arrange
        var pin = "123456";
        var session = new Session
        {
            PIN = pin,
            SessionName = "Test Session",
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var participant = new Participant
        {
            SessionId = session.SessionId,
            Name = "John Doe",
            JoinedAt = DateTime.UtcNow
        };
        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();

        var existingVote = new Vote
        {
            SessionId = session.SessionId,
            ParticipantId = participant.ParticipantId,
            CardValue = "3",
            VotedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _context.Votes.Add(existingVote);
        await _context.SaveChangesAsync();

        var request = new SubmitVoteRequest
        {
            ParticipantId = participant.ParticipantId,
            CardValue = "8"
        };

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.SubmitVote(pin, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var votes = await _context.Votes
            .Where(v => v.ParticipantId == participant.ParticipantId)
            .ToListAsync();
        
        Assert.Single(votes);
        Assert.Equal("8", votes[0].CardValue);
    }

    [Fact]
    public async Task RevealVotes_ValidPin_ReturnsOk()
    {
        // Arrange
        var pin = "123456";
        var session = new Session
        {
            PIN = pin,
            SessionName = "Test Session",
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.RevealVotes(pin);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        _clientProxyMock.Verify(
            c => c.SendCoreAsync("VotesRevealed", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task RevealVotes_InvalidPin_ReturnsBadRequest()
    {
        // Arrange
        var invalidPin = "invalid";

        _pinGeneratorMock.Setup(p => p.IsValidPin(invalidPin)).Returns(false);

        // Act
        var result = await _controller.RevealVotes(invalidPin);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid PIN format", badRequestResult.Value);
    }

    [Fact]
    public async Task RevealVotes_NonExistentSession_ReturnsNotFound()
    {
        // Arrange
        var pin = "999999";

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.RevealVotes(pin);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Session not found", notFoundResult.Value);
    }

    [Fact]
    public async Task GetResults_ValidPin_ReturnsOkWithResults()
    {
        // Arrange
        var pin = "123456";
        var session = new Session
        {
            PIN = pin,
            SessionName = "Test Session",
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var participant1 = new Participant
        {
            SessionId = session.SessionId,
            Name = "John Doe",
            JoinedAt = DateTime.UtcNow
        };
        var participant2 = new Participant
        {
            SessionId = session.SessionId,
            Name = "Jane Smith",
            JoinedAt = DateTime.UtcNow
        };
        _context.Participants.AddRange(participant1, participant2);
        await _context.SaveChangesAsync();

        var vote1 = new Vote
        {
            SessionId = session.SessionId,
            ParticipantId = participant1.ParticipantId,
            CardValue = "5",
            VotedAt = DateTime.UtcNow
        };
        var vote2 = new Vote
        {
            SessionId = session.SessionId,
            ParticipantId = participant2.ParticipantId,
            CardValue = "8",
            VotedAt = DateTime.UtcNow
        };
        _context.Votes.AddRange(vote1, vote2);
        await _context.SaveChangesAsync();

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.GetResults(pin);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var resultsDto = Assert.IsType<ResultsDto>(okResult.Value);
        
        Assert.Equal(2, resultsDto.Votes.Count);
        Assert.NotNull(resultsDto.Statistics);
        Assert.Equal(6.5, resultsDto.Statistics.Average);
        Assert.Equal(5, resultsDto.Statistics.Min);
        Assert.Equal(8, resultsDto.Statistics.Max);
    }

    [Fact]
    public async Task GetResults_WithInfinitySymbol_ExcludesFromStatistics()
    {
        // Arrange
        var pin = "123456";
        var session = new Session
        {
            PIN = pin,
            SessionName = "Test Session",
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var participant1 = new Participant
        {
            SessionId = session.SessionId,
            Name = "John Doe",
            JoinedAt = DateTime.UtcNow
        };
        var participant2 = new Participant
        {
            SessionId = session.SessionId,
            Name = "Jane Smith",
            JoinedAt = DateTime.UtcNow
        };
        _context.Participants.AddRange(participant1, participant2);
        await _context.SaveChangesAsync();

        var vote1 = new Vote
        {
            SessionId = session.SessionId,
            ParticipantId = participant1.ParticipantId,
            CardValue = "5",
            VotedAt = DateTime.UtcNow
        };
        var vote2 = new Vote
        {
            SessionId = session.SessionId,
            ParticipantId = participant2.ParticipantId,
            CardValue = "∞",
            VotedAt = DateTime.UtcNow
        };
        _context.Votes.AddRange(vote1, vote2);
        await _context.SaveChangesAsync();

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.GetResults(pin);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var resultsDto = Assert.IsType<ResultsDto>(okResult.Value);
        
        Assert.Equal(2, resultsDto.Votes.Count);
        Assert.NotNull(resultsDto.Statistics);
        Assert.Equal(5.0, resultsDto.Statistics.Average);
        Assert.Equal(5, resultsDto.Statistics.Min);
        Assert.Equal(5, resultsDto.Statistics.Max);
    }

    [Fact]
    public async Task GetResults_NoNumericVotes_ReturnsNullStatistics()
    {
        // Arrange
        var pin = "123456";
        var session = new Session
        {
            PIN = pin,
            SessionName = "Test Session",
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var participant = new Participant
        {
            SessionId = session.SessionId,
            Name = "John Doe",
            JoinedAt = DateTime.UtcNow
        };
        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();

        var vote = new Vote
        {
            SessionId = session.SessionId,
            ParticipantId = participant.ParticipantId,
            CardValue = "∞",
            VotedAt = DateTime.UtcNow
        };
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.GetResults(pin);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var resultsDto = Assert.IsType<ResultsDto>(okResult.Value);
        
        Assert.Single(resultsDto.Votes);
        Assert.Null(resultsDto.Statistics);
    }

    [Fact]
    public async Task GetResults_InvalidPin_ReturnsBadRequest()
    {
        // Arrange
        var invalidPin = "invalid";

        _pinGeneratorMock.Setup(p => p.IsValidPin(invalidPin)).Returns(false);

        // Act
        var result = await _controller.GetResults(invalidPin);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid PIN format", badRequestResult.Value);
    }

    [Fact]
    public async Task GetResults_NonExistentSession_ReturnsNotFound()
    {
        // Arrange
        var pin = "999999";

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.GetResults(pin);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Session not found", notFoundResult.Value);
    }

    [Fact]
    public async Task GetResults_MultipleVotesPerParticipant_ReturnsLatestVote()
    {
        // Arrange
        var pin = "123456";
        var session = new Session
        {
            PIN = pin,
            SessionName = "Test Session",
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var participant = new Participant
        {
            SessionId = session.SessionId,
            Name = "John Doe",
            JoinedAt = DateTime.UtcNow
        };
        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();

        var oldVote = new Vote
        {
            SessionId = session.SessionId,
            ParticipantId = participant.ParticipantId,
            CardValue = "3",
            VotedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var newVote = new Vote
        {
            SessionId = session.SessionId,
            ParticipantId = participant.ParticipantId,
            CardValue = "8",
            VotedAt = DateTime.UtcNow
        };
        _context.Votes.AddRange(oldVote, newVote);
        await _context.SaveChangesAsync();

        _pinGeneratorMock.Setup(p => p.IsValidPin(pin)).Returns(true);

        // Act
        var result = await _controller.GetResults(pin);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var resultsDto = Assert.IsType<ResultsDto>(okResult.Value);
        
        Assert.Single(resultsDto.Votes);
        Assert.Equal("8", resultsDto.Votes[0].CardValue);
    }
}
