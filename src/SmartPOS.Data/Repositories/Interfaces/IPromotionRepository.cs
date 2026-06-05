using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface IPromotionRepository
{
    Task<Promotion?> GetByCodeAsync(string code); Task<IReadOnlyList<Promotion>> GetActiveAsync(DateOnly date); Task AddAsync(Promotion promotion); Task UpdateAsync(Promotion promotion);
}

