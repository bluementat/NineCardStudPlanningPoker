using Microsoft.EntityFrameworkCore;
using PlanningPoker.Api.Data;

namespace PlanningPoker.Api.Services;

public interface IPinGenerator
{
    Task<string> GenerateUniquePinAsync();
    bool IsValidPin(string pin);
}

public class PinGenerator : IPinGenerator
{
    private readonly PlanningPokerDbContext _context;
    private readonly Random _random = new();

    public PinGenerator(PlanningPokerDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateUniquePinAsync()
    {
        string pin;
        int attempts = 0;
        const int maxAttempts = 100;

        do
        {
            pin = _random.Next(100000, 999999).ToString();
            attempts++;

            if (attempts > maxAttempts)
            {
                throw new InvalidOperationException("Unable to generate unique PIN after multiple attempts.");
            }
        }
        while (await _context.Sessions.AnyAsync(s => s.PIN == pin));

        return pin;
    }

    public bool IsValidPin(string pin)
    {
        return !string.IsNullOrWhiteSpace(pin) 
            && pin.Length == 6 
            && pin.All(char.IsDigit);
    }
}
