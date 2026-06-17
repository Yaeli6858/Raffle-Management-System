using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models;
using server.Repositories.Interfaces;

namespace server.Repositories.Implementations;

public class WinningRepository : IWinningRepository
{
    private readonly AppDbContext _context;

    public WinningRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WinningModel>> GetAllWinningsAsync()
    {
        return await _context.Winnings
            .Include(w => w.Gift)
            .Include(w => w.User)
            .Take(100).ToListAsync();
    }

    public async Task<WinningModel?> GetWinningByIdAsync(int id)
    {
        return await _context.Winnings
            .Include(w => w.Gift)
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.Id == id);
    }


    public async Task<WinningModel> AddWinningAsync(WinningModel winning)
    {
        _context.Winnings.Add(winning);
        await _context.SaveChangesAsync();
        return winning;
    }

    public async Task<WinningModel?> UpdateWinningAsync(int id, WinningModel winning)
    {
        var existing = await _context.Winnings.FindAsync(id);
        if (existing == null)
            return null;

        existing.GiftId = winning.GiftId;
        existing.WinnerId = winning.WinnerId;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteWinningAsync(int id)
    {
        var winning = await _context.Winnings.FindAsync(id);
        if (winning == null)
            return false;

        _context.Winnings.Remove(winning);
        await _context.SaveChangesAsync();
        return true;
    }

}