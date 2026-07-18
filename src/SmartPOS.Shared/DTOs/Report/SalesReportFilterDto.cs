using SmartPOS.Shared.Enums;

namespace SmartPOS.Shared.DTOs.Report;

public record SalesReportFilterDto(
    DateTime       FromDate,
    DateTime       ToDate,
    Guid?          StaffId           = null,
    Guid?          ShiftId           = null,
    PaymentMethod? PaymentMethod     = null,
    Guid?          RequestedByUserId = null);
