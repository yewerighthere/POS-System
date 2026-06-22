using Microsoft.EntityFrameworkCore;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class PromotionRepository : IPromotionRepository
{
    private readonly AppDbContext _context;

    public PromotionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Promotion?> GetByIdAsync(Guid id)
    {
        return await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Promotion?> GetByCodeAsync(string code)
    {
        return await _context.Promotions.FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task<Promotion?> GetByNameAsync(string name)
    {
        return await _context.Promotions.FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<IReadOnlyList<Promotion>> GetAllPromotionsAsync()
    {
        return await _context.Promotions.ToListAsync();
    }

    public async Task<IReadOnlyList<Promotion>> GetExpiredPromotionsAsync(DateOnly currentDate)
    {
        return await _context.Promotions.Where(p => p.EndDate < currentDate).ToListAsync();
    }

    public async Task<IReadOnlyList<Promotion>> GetUnexpiredPromotionsAsync(DateOnly currentDate)
    {
        return await _context.Promotions.Where(p => p.EndDate >= currentDate).ToListAsync();
    }

    public async Task<IReadOnlyList<Promotion>> SearchByNameAsync(string nameKeyword)
    {
        return await _context.Promotions.Where(p => p.Name.Contains(nameKeyword)).ToListAsync();
    }

    public async Task<IReadOnlyList<Promotion>> SearchByCodeAsync(string codeKeyword)
    {
        return await _context.Promotions.Where(p => p.Code.Contains(codeKeyword)).ToListAsync();
    }

    public async Task<IReadOnlyList<Promotion>> GetActiveAsync(DateOnly date)
    {
        return await _context.Promotions.Where(p => p.IsActive && p.StartDate <= date && p.EndDate >= date).ToListAsync();
    }

    public async Task AddAsync(Promotion promotion)
    {
        await _context.Promotions.AddAsync(promotion);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Promotion promotion)
    {
        _context.Promotions.Update(promotion);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var promotion = await GetByIdAsync(id);
        if (promotion != null)
        {
            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateCodeAsync(Guid id, string code)
    {
        var p = await GetByIdAsync(id);
        if (p != null) { p.Code = code; await _context.SaveChangesAsync(); }
    }

    public async Task UpdateNameAsync(Guid id, string name)
    {
        var p = await GetByIdAsync(id);
        if (p != null) { p.Name = name; await _context.SaveChangesAsync(); }
    }

    public async Task UpdateDescriptionAsync(Guid id, string? description)
    {
        var p = await GetByIdAsync(id);
        if (p != null) { p.Description = description; await _context.SaveChangesAsync(); }
    }

    public async Task UpdateTypeAsync(Guid id, string type)
    {
        var p = await GetByIdAsync(id);
        if (p != null) { p.Type = type; await _context.SaveChangesAsync(); }
    }

    public async Task UpdateDiscountValueAsync(Guid id, decimal discountValue)
    {
        var p = await GetByIdAsync(id);
        if (p != null) { p.DiscountValue = discountValue; await _context.SaveChangesAsync(); }
    }

    public async Task UpdateMinOrderAmountAsync(Guid id, decimal? minOrderAmount)
    {
        var p = await GetByIdAsync(id);
        if (p != null) { p.MinOrderAmount = minOrderAmount; await _context.SaveChangesAsync(); }
    }

    public async Task UpdateProductIdAsync(Guid id, Guid? productId)
    {
        var p = await GetByIdAsync(id);
        if (p != null) { p.ProductId = productId; await _context.SaveChangesAsync(); }
    }

    public async Task UpdateStartDateAsync(Guid id, DateOnly startDate)
    {
        var p = await GetByIdAsync(id);
        if (p != null) { p.StartDate = startDate; await _context.SaveChangesAsync(); }
    }

    public async Task UpdateEndDateAsync(Guid id, DateOnly endDate)
    {
        var p = await GetByIdAsync(id);
        if (p != null) { p.EndDate = endDate; await _context.SaveChangesAsync(); }
    }

    public async Task UpdateRequiresApprovalThresholdAsync(Guid id, decimal? threshold)
    {
        var p = await GetByIdAsync(id);
        if (p != null) { p.RequiresApprovalThreshold = threshold; await _context.SaveChangesAsync(); }
    }

    public async Task UpdateIsActiveAsync(Guid id, bool isActive)
    {
        var p = await GetByIdAsync(id);
        if (p != null) { p.IsActive = isActive; await _context.SaveChangesAsync(); }
    }
}
