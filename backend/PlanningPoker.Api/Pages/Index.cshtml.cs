using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PlanningPoker.Api.Data;
using PlanningPoker.Api.Models;
using PlanningPoker.Api.Services;

namespace PlanningPoker.Api.Pages;

public class IndexModel : PageModel
{
    private readonly PlanningPokerDbContext _context;
    private readonly IPinGenerator _pinGenerator;

    public IndexModel(PlanningPokerDbContext context, IPinGenerator pinGenerator)
    {
        _context = context;
        _pinGenerator = pinGenerator;
    }

    [BindProperty]
    public string? SessionName { get; set; }

    [BindProperty]
    public string? ParticipantName { get; set; }

    [BindProperty]
    public string? Pin { get; set; }

    public bool ShowCreate { get; set; }
    public bool ShowJoin { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public void OnPostShowCreate()
    {
        ShowCreate = true;
    }

    public void OnPostShowJoin()
    {
        ShowJoin = true;
    }

    public void OnPostCancel()
    {
        ShowCreate = false;
        ShowJoin = false;
    }

    public async Task<IActionResult> OnPostCreateSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionName) || string.IsNullOrWhiteSpace(ParticipantName))
        {
            ErrorMessage = "Session name and your name are required.";
            ShowCreate = true;
            return Page();
        }

        var pin = await _pinGenerator.GenerateUniquePinAsync();
        var session = new Session
        {
            PIN = pin,
            SessionName = SessionName,
            CreatedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var participant = new Participant
        {
            SessionId = session.SessionId,
            Name = ParticipantName,
            JoinedAt = DateTime.UtcNow
        };

        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();

        // Set the host
        session.HostParticipantId = participant.ParticipantId;
        await _context.SaveChangesAsync();

        // Store participant info in session/cookie for the next page
        HttpContext.Session.SetString("ParticipantName", participant.Name);
        HttpContext.Session.SetInt32("ParticipantId", participant.ParticipantId);
        HttpContext.Session.SetString("IsNewSession", "true");

        return RedirectToPage("/Session", new { pin = session.PIN });
    }

    public async Task<IActionResult> OnPostJoinSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(Pin) || string.IsNullOrWhiteSpace(ParticipantName))
        {
            ErrorMessage = "PIN and your name are required.";
            ShowJoin = true;
            return Page();
        }

        if (!_pinGenerator.IsValidPin(Pin))
        {
            ErrorMessage = "Invalid PIN format.";
            ShowJoin = true;
            return Page();
        }

        var session = await _context.Sessions.FirstOrDefaultAsync(s => s.PIN == Pin);
        if (session == null)
        {
            ErrorMessage = "Session not found.";
            ShowJoin = true;
            return Page();
        }

        if (session.Status != SessionStatus.Active)
        {
            ErrorMessage = "Session is no longer active.";
            ShowJoin = true;
            return Page();
        }

        var participant = new Participant
        {
            SessionId = session.SessionId,
            Name = ParticipantName,
            JoinedAt = DateTime.UtcNow
        };

        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();

        HttpContext.Session.SetString("ParticipantName", participant.Name);
        HttpContext.Session.SetInt32("ParticipantId", participant.ParticipantId);
        HttpContext.Session.SetString("IsNewSession", "false");

        return RedirectToPage("/Session", new { pin = session.PIN });
    }
}
