using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using System.Text.Json;

namespace SmartPOS.Services.Implementations;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditLogRepository auditLogRepository, ILogger<AuditService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task LogAsync(string action, string? entity, Guid? entityId, object? oldValue, object? newValue, Guid userId)
    {
        try
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
                NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
                CreatedAt = DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(log);
        }
        catch (Exception ex)
        {
            // Ghi log lỗi nhưng không để audit làm crash flow chính
            _logger.LogError(ex, "Không thể ghi audit log cho action {Action} entity {Entity}", action, entity);
        }
    }
}
