using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models;
using server.Repositories.Interfaces;


namespace server.Repositories.Implementations;
public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CategoryModel>> GetAllCategoriesAsync()
    {
        return await _context.Categories.Take(100).ToListAsync();
    }

    public async Task<CategoryModel?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories.FindAsync(id);
    }

    public async Task<CategoryModel> AddCategoryAsync(CategoryModel category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<CategoryModel?> UpdateCategoryAsync(CategoryModel category)
    {
        var existingCategory = await _context.Categories.FindAsync(category.Id);
        if (existingCategory == null) return null;
        _context.Entry(existingCategory).CurrentValues.SetValues(category);
        await _context.SaveChangesAsync();
        return existingCategory;
    }

    public async Task<bool> HasGiftsAsync(int categoryId)
    {
        return await _context.Gifts
            .AnyAsync(p => p.CategoryId == categoryId);
    }
    
    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return false;
        }
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }
}