using Microsoft.Extensions.Logging;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Customer;

namespace SmartPOS.Services.Implementations;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ICustomerRepository customerRepository, ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<CustomerDto?> FindByPhoneAsync(string phone)
    {
        var entity = await _customerRepository.GetByPhoneAsync(phone).ConfigureAwait(false);
        if (entity == null) return null;
        
        return new CustomerDto
        {
            Id = entity.Id,
            FullName = entity.FullName,
            Phone = entity.Phone,
            MemberCode = entity.MemberCode,
            LoyaltyPoints = entity.LoyaltyPoints
        };
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        var existing = await _customerRepository.GetByPhoneAsync(dto.Phone).ConfigureAwait(false);
        if (existing != null) 
            throw new SmartPOS.Shared.Exceptions.BusinessException("Số điện thoại đã tồn tại");
        
        var newCustomer = new SmartPOS.Data.Entities.Customer
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Phone = dto.Phone,
            Email = dto.Email,
            LoyaltyPoints = 0,
            CreatedAt = DateTime.UtcNow
        };
        
        await _customerRepository.AddAsync(newCustomer).ConfigureAwait(false);
        
        return new CustomerDto
        {
            Id = newCustomer.Id,
            FullName = newCustomer.FullName,
            Phone = newCustomer.Phone,
            MemberCode = newCustomer.MemberCode,
            LoyaltyPoints = newCustomer.LoyaltyPoints
        };
    }

    public Task AddLoyaltyPointsAsync(Guid customerId, int points)
        => throw new NotImplementedException();

    public int CalculatePoints(decimal subtotal)
        => throw new NotImplementedException();
}
