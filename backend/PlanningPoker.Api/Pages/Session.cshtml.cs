using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PlanningPoker.Api.Data;
using PlanningPoker.Api.Models;

namespace PlanningPoker.Api.Pages;

public class SessionModel : PageModel
{
    private readonly PlanningPokerDbContext _context;

    public SessionModel(PlanningPokerDbContext context)
    {
        _context = context;
    }

    public Session? Session { get; set; }
    public string? ParticipantName { get; set; }
    public int? ParticipantId { get; set; }
    public string? CurrentVote { get; set; }
    public bool IsRevealed { get; set; }
    public bool IsHost { get; set; }

    public async Task<IActionResult> OnGetAsync(string pin)
    {
        ParticipantName = HttpContext.Session.GetString("ParticipantName");
        ParticipantId = HttpContext.Session.GetInt32("ParticipantId");

        if (string.IsNullOrEmpty(ParticipantName) || ParticipantId == null)
        {
            return RedirectToPage("/Index");
        }

        Session = await _context.Sessions
            .Include(s => s.Participants)
            .Include(s => s.Votes)
            .FirstOrDefaultAsync(s => s.PIN == pin);

        if (Session == null)
        {
            return RedirectToPage("/Index");
        }

        IsHost = Session.HostParticipantId == ParticipantId;

        var myVote = Session.Votes.FirstOrDefault(v => v.ParticipantId == ParticipantId);
        CurrentVote = myVote?.CardValue;

        // In a real app, you might store the revealed state in the DB. 
        // For now, we'll assume it's not revealed on initial load unless we add a Status to Session.
        IsRevealed = false; 

        // Clear the new session flag after reading it once
        HttpContext.Session.Remove("IsNewSession");

        return Page();
    }
}
