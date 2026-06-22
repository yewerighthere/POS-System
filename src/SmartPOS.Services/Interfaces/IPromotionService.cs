using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.DTOs.Shift;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Order;
using SmartPOS.Shared.DTOs.Payment;
using SmartPOS.Shared.DTOs.Invoice;
using SmartPOS.Shared.DTOs.Customer;
using SmartPOS.Shared.DTOs.Return;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Inventory;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.DTOs.Promotion;
using SmartPOS.Shared.Enums;

namespace SmartPOS.Services.Interfaces;

public interface IPromotionService
{
    Task<PromotionValidationResultDto> ValidateCodeAsync(string code, CartSummaryDto cart);
    Task<bool> RequestApprovalAsync(Guid promotionId, Guid managerId);
    Task<CartSummaryDto> ApplyPromotionAsync(string code, CartSummaryDto cart);
    Task<PromotionDto> CreatePromotionAsync(SmartPOS.Shared.DTOs.Promotion.CreatePromotionDto dto);

    // GET & SEARCH
    Task<PromotionDto?> GetByIdAsync(Guid id);
    Task<PromotionDto?> GetByCodeAsync(string code);
    Task<PromotionDto?> GetByNameAsync(string name);
    Task<IReadOnlyList<PromotionDto>> GetAllPromotionsAsync();
    Task<IReadOnlyList<PromotionDto>> GetExpiredPromotionsAsync(DateOnly currentDate);
    Task<IReadOnlyList<PromotionDto>> GetUnexpiredPromotionsAsync(DateOnly currentDate);
    Task<IReadOnlyList<PromotionDto>> SearchByNameAsync(string nameKeyword);
    Task<IReadOnlyList<PromotionDto>> SearchByCodeAsync(string codeKeyword);
    Task<IReadOnlyList<PromotionDto>> GetActiveAsync(DateOnly date);

    // CRUD
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
