using Microsoft.EntityFrameworkCore;
using server.Data;
using server.DTOs;
using server.Models;
using server.Models.Enums;
using server.Repositories.Interfaces;


namespace server.Repositories.Implementations;


public class GiftRepository : IGiftRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<GiftRepository> _logger;

    public GiftRepository(AppDbContext context, ILogger<GiftRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<GiftModel>> GetAllGiftsAsync(
        int? categoryId,
        int? donorId,
        PriceSort sort)
    {
        IQueryable<GiftModel> query = _context.Gifts
            .Include(g => g.Category);

        // פילטור לפי קטגוריה
        if (categoryId.HasValue)
        {
            query = query.Where(g => g.CategoryId == categoryId.Value);
        }

        // פילטור לפי תורם
        if (donorId.HasValue)
        {
            query = query.Where(g => g.DonorId == donorId.Value);
        }

        // מיון לפי מחיר
        query = sort switch
        {
            PriceSort.Ascending => query.OrderBy(g => g.Price),
            PriceSort.Descending => query.OrderByDescending(g => g.Price),
            _ => query
        };

        return await query.Take(100).ToListAsync();
    }

    public async Task<IEnumerable<GiftModel>> GetGiftsAsync(PriceSort sort)
    {
        IQueryable<GiftModel> query = _context.Gifts.Include(g => g.Category);

        //מיון לפי מחיר
        switch (sort)
        {
            case PriceSort.Ascending:
                query = query.OrderBy(g => g.Price);
                break;
            case PriceSort.Descending:
                query = query.OrderByDescending(g => g.Price);
                break;
            default:
                break;
        }
        return await query.Take(100).ToListAsync();


    }

    public async Task<IEnumerable<GiftModel>> GetGiftByCategoryAsync(int categoryId)
    {
        return await _context.Gifts
        .Where(g => g.CategoryId == categoryId)
        .Include(g => g.Category)
        .Take(100).ToListAsync();
    }

    public async Task<IEnumerable<GiftModel>> GetByDonorAsync(int donorId)
    {
        return await _context.Gifts
            .Where(g => g.DonorId == donorId)
            .Include(g => g.Category)
            .Take(100).ToListAsync();
    }



    public async Task<GiftModel?> GetGiftByIdAsync(int id)
    {
        return await _context.Gifts
        .Include(g => g.Category)
        .FirstOrDefaultAsync(g => g.Id == id);
    }




    public async Task<GiftModel> AddGiftAsync(GiftModel gift)
    {
        _context.Gifts.Add(gift);
        await _context.SaveChangesAsync();

        return await _context.Gifts
            .Include(g => g.Category)
            .FirstAsync(g => g.Id == gift.Id);
    }


    public async Task<GiftModel?> UpdateGiftAsync(GiftModel gift)
    {
        // _logger.LogInformation($"Updating gift with ID {gift.Id}. hasWinning: {gift.HasWinning}");
        var existingGift = await _context.Gifts.FindAsync(gift.Id);
        if (existingGift == null)
        {
            return await AddGiftAsync(gift);
        }


        _context.Entry(existingGift).CurrentValues.SetValues(gift);
        await _context.SaveChangesAsync();

        await _context.Entry(existingGift)
            .Reference(g => g.Category)
            .LoadAsync();
        // _logger.LogInformation($"Gift {gift.Id} updated successfully. hasWinning: {existingGift.HasWinning}");


        return existingGift;

    }

    public async Task<bool> HasPurchasesAsync(int productId)
    {
        return await _context.Purchases
            .AnyAsync(p => p.GiftId == productId);
    }
    public async Task<bool> DeleteGiftAsync(int id)
    {
        var gift = await _context.Gifts.FindAsync(id);
        if (gift == null)
        {
            return false;
        }
        _context.Gifts.Remove(gift);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<GiftModel>> FilterByGiftName(string name)
    {
        return await _context.Gifts
        .Where(g => g.Description.Contains(name))
        .Include(g => g.Category)
        .Take(100).ToListAsync();
    }

    public async Task<IEnumerable<GiftModel>> FilterByGiftDonor(string name)
    {
        return await _context.Gifts
        .Include(g => g.Donor)
        .Include(g => g.Category)
        .Where(g => g.Donor.Name.Contains(name))
        .Take(100).ToListAsync();
    }

    public async Task<IEnumerable<GiftPurchaseCountDto>> GetPurchaseCountByGiftAsync()
    {
        return await _context.Gifts
            .GroupJoin(
                _context.Purchases,
                gift => gift.Id,
                purchase => purchase.GiftId,
                (gift, purchases) => new GiftPurchaseCountDto
                {
                    GiftId = gift.Id,
                    GiftName = gift.Description,
                    PurchaseCount = purchases.Count(p => p.Status == Status.Completed),
                    DonorName = gift.Donor.Name
                })
            .Take(100).ToListAsync();
    }

    public async Task<bool> ExistsByDescriptionAsync(string description)
    {
        return await _context.Gifts
            .AnyAsync(g => g.Description.ToLower() == description.ToLower());
    }




}