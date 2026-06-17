using Microsoft.EntityFrameworkCore;
using server.Data;
using server.DTOs;
using server.Models;
using server.Repositories.Interfaces;

namespace server.Repositories.Implementations;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly AppDbContext _context;

    public PurchaseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PurchaseModel>> GetAllAsync()
        => await _context.Purchases
            .Include(p => p.Gift)
            .Include(p => p.User)
            .Take(100).ToListAsync();

    public async Task<IEnumerable<PurchaseModel>> GetByGiftIdsAsync(IEnumerable<int> giftIds)
    {
           return await _context.Purchases
        .Where(p =>
            giftIds.Contains(p.GiftId) &&
            p.Status == Status.Completed)
        .Take(100).ToListAsync();
    }


    public async Task<PurchaseModel?> GetByIdAsync(int id)
        => await _context.Purchases
            .Include(p => p.Gift)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<PurchaseModel> AddAsync(PurchaseModel purchase)
    {
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        return await _context.Purchases
            .Include(p => p.Gift)
            .Include(p => p.User)
            .FirstAsync(p => p.Id == purchase.Id);
    }

    public async Task<PurchaseModel?> UpdateAsync(PurchaseModel purchase)
    {
        var existing = await _context.Purchases.FindAsync(purchase.Id);
        if (existing == null) return null;

        _context.Entry(existing).CurrentValues.SetValues(purchase);
        await _context.SaveChangesAsync();
        return await _context.Purchases
            .Include(p => p.Gift)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == existing.Id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _context.Purchases.FindAsync(id);
        if (existing == null) return false;
        if (existing.Status == Status.Completed) return false;

        _context.Purchases.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<PurchaseModel>> GetByGiftAsync(int giftId)
        => await _context.Purchases
            .Include(p => p.Gift)
            .Include(p => p.User)
            .Where(p => p.GiftId == giftId && p.Status == Status.Completed)
            .Take(100).ToListAsync();

    public async Task<IEnumerable<GiftPurchaseCountDto>> GetPurchaseCountByGiftAsync()
    {
        return await _context.Purchases
            .GroupBy(p => p.GiftId)
            .Select(g => new GiftPurchaseCountDto
            {
                GiftId = g.Key,
                GiftName = g.First().Gift.Description,
                PurchaseCount = g.Count(),
                DonorName = g.First().Gift.Donor.Name
            })
            .Take(100).ToListAsync();
    }


    public async Task<List<PurchaseModel>> GetUserCartAsync(int userId)
        => await _context.Purchases
            .Include(p => p.Gift)
            .Include(p => p.User)
            .Where(p => p.UserId == userId && p.Status == Status.Draft)
            .Take(100).ToListAsync();


    public async Task<PurchaseModel?> FindDraftByUserAndGift(int userId, int giftId)
    {
        return await _context.Purchases
            .FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.GiftId == giftId &&
                p.Status == Status.Draft);
    }

}
