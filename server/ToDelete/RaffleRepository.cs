// using server.Models;
// using server.Repositories.Interfaces;
// using Microsoft.EntityFrameworkCore;
// using server.Data;

// namespace server.Repositories.Implementations;

// public class RaffleRepository : IRaffleRepository
// {
//     private readonly AppDbContext _context;

//     public RaffleRepository(AppDbContext context)
//     {
//         _context = context;
//     }

//     public async Task<List<RaffleModel>> GetAllAsync()
//     {
//         return await _context.Raffles.Take(100).ToListAsync();
//     }

//     public async Task<RaffleModel?> GetByIdAsync(int id)
//     {
//         return await _context.Raffles.FindAsync(id);
//     }

//     public async Task<RaffleModel> AddAsync(RaffleModel raffle)
//     {
//         _context.Raffles.Add(raffle);
//         await _context.SaveChangesAsync();
//         return raffle;
//     }

//     public async Task<RaffleModel> UpdateAsync(RaffleModel raffle)
//     {
//         var existing = await _context.Raffles.FindAsync(raffle.Id);
//         if (existing == null)
//             throw new KeyNotFoundException("Raffle not found");

//         _context.Entry(existing).CurrentValues.SetValues(raffle);
//         await _context.SaveChangesAsync();
//         return existing;
//     }

//     public async Task<bool> DeleteAsync(int id)
//     {
//         var raffle = await _context.Raffles.FindAsync(id);
//         if (raffle == null)
//             return false;

//         _context.Raffles.Remove(raffle);
//         await _context.SaveChangesAsync();
//         return true;
//     }


//     public async Task<List<WinningModel>> GetWinningsByRaffleAsync(int raffleId)
//     {
//         return await _context.Winnings
//             .Where(w => w.RaffleId == raffleId)
//             .Take(100).ToListAsync();
//     }
// }