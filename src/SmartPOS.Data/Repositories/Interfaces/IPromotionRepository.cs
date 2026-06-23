using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IPromotionRepository
{
    // GET & SEARCH
    Task<Promotion?> GetByIdAsync(Guid id);
    Task<Promotion?> GetByCodeAsync(string code);
    Task<Promotion?> GetByNameAsync(string name);
    Task<IReadOnlyList<Promotion>> GetAllPromotionsAsync();
    Task<IReadOnlyList<Promotion>> GetExpiredPromotionsAsync(DateOnly currentDate);
    Task<IReadOnlyList<Promotion>> GetUnexpiredPromotionsAsync(DateOnly currentDate);
    Task<IReadOnlyList<Promotion>> SearchByNameAsync(string nameKeyword);
    Task<IReadOnlyList<Promotion>> SearchByCodeAsync(string codeKeyword);
    Task<IReadOnlyList<Promotion>> GetActiveAsync(DateOnly date);

    // CRUD
    Task AddAsync(Promotion promotion);
    Task UpdateAsync(Promotion promotion);
    Task DeleteAsync(Guid id);

    // INDIVIDUAL UPDATES
    Task UpdateCodeAsync(Guid id, string code);
    Task UpdateNameAsync(Guid id, string name);
    Task UpdateDescriptionAsync(Guid id, string? description);
    Task UpdateTypeAsync(Guid id, string type);
    Task UpdateDiscountValueAsync(Guid id, decimal discountValue);
    Task UpdateMinOrderAmountAsync(Guid id, decimal? minOrderAmount);
    Task UpdateProductIdAsync(Guid id, Guid? productId);
    Task UpdateStartDateAsync(Guid id, DateOnly startDate);
    Task UpdateEndDateAsync(Guid id, DateOnly endDate);
    Task UpdateRequiresApprovalThresholdAsync(Guid id, decimal? threshold);
    Task UpdateIsActiveAsync(Guid id, bool isActive);
}
