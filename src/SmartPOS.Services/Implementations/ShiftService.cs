using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Shift;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;

namespace SmartPOS.Services.Implementations;

public class ShiftService : IShiftService
{
    private readonly IShiftRepository _shiftRepo;
    private readonly IUserRepository _userRepo;
    private readonly ILogger<ShiftService> _logger;

    public ShiftService(IShiftRepository shiftRepo, IUserRepository userRepo, ILogger<ShiftService> logger)
    {
        _shiftRepo = shiftRepo;
        _userRepo = userRepo;
        _logger = logger;
    }

    public async Task<ShiftDto> OpenShiftAsync(OpenShiftDto dto)
    {
        if (dto.OpeningCash < 0)
            throw new BusinessException("Tiền đầu ca không được âm");

        var user = await _userRepo.GetByIdAsync(dto.UserId).ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy người dùng");
        if (user.Status == UserStatus.Locked)
            throw new BusinessException("Tài khoản đã bị khóa, không thể mở ca");

        var existing = await _shiftRepo.GetOpenShiftAsync(dto.UserId).ConfigureAwait(false);
        if (existing is not null)
            throw new BusinessException("Bạn đang có ca làm việc đang mở");

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Status = ShiftStatus.Open,
            OpeningCash = dto.OpeningCash,
            OpenedAt = DateTime.UtcNow
        };

        await _shiftRepo.AddAsync(shift).ConfigureAwait(false);
        _logger.LogInformation("Đã mở ca {ShiftId} cho nhân viên {UserId}", shift.Id, dto.UserId);

        return MapToDto(shift);
    }

    public async Task<ShiftDto> CloseShiftAsync(CloseShiftDto dto)
    {
        var shift = await _shiftRepo.GetByIdAsync(dto.ShiftId).ConfigureAwait(false)
            ?? throw new BusinessException("Ca không tồn tại");

        if (shift.Status != ShiftStatus.Open)
            throw new BusinessException("Ca đã được đóng");

        if (dto.ClosingCash < 0)
            throw new BusinessException("Tiền cuối ca không được âm");

        var cashRevenue = await _shiftRepo.GetCashRevenueAsync(dto.ShiftId).ConfigureAwait(false);
        var expectedCash = shift.OpeningCash + cashRevenue;

        shift.Status = ShiftStatus.Closed;
        shift.ClosingCash = dto.ClosingCash;
        shift.ExpectedCash = expectedCash;
        shift.CashDifference = dto.ClosingCash - expectedCash;
        shift.ClosedAt = DateTime.UtcNow;

        await _shiftRepo.UpdateAsync(shift).ConfigureAwait(false);
        _logger.LogInformation("Đã đóng ca {ShiftId}, chênh lệch tiền mặt: {Diff}", shift.Id, shift.CashDifference);

        return MapToDto(shift);
    }

    public async Task<ShiftDto?> GetOpenShiftAsync(Guid userId)
    {
        var shift = await _shiftRepo.GetOpenShiftAsync(userId).ConfigureAwait(false);
        return shift is null ? null : MapToDto(shift);
    }

    public async Task<ShiftSummaryDto> GetShiftSummaryAsync(Guid shiftId)
    {
        var shift = await _shiftRepo.GetByIdAsync(shiftId).ConfigureAwait(false)
            ?? throw new BusinessException("Ca không tồn tại");

        var totalSales = await _shiftRepo.GetTotalSalesAsync(shiftId).ConfigureAwait(false);
        var cashRevenue = await _shiftRepo.GetCashRevenueAsync(shiftId).ConfigureAwait(false);

        return new ShiftSummaryDto
        {
            ShiftId       = shiftId,
            TotalSales    = totalSales,
            ExpectedCash  = shift.OpeningCash + cashRevenue,
            OpeningCash   = shift.OpeningCash,
            ClosingCash   = shift.ClosingCash ?? 0,
            CashDifference = shift.CashDifference ?? 0
        };
    }

    private static ShiftDto MapToDto(Shift shift) => new()
    {
        Id = shift.Id,
        UserId = shift.UserId,
        Status = shift.Status.ToString(),
        OpeningCash = shift.OpeningCash,
        OpenedAt = shift.OpenedAt
    };
}
