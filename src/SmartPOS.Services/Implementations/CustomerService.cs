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

    public Task<CustomerDto?> FindByPhoneAsync(string phone)
        => throw new NotImplementedException();

    public Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
        => throw new NotImplementedException();

    public Task AddLoyaltyPointsAsync(Guid customerId, int points)
        => throw new NotImplementedException();

    public int CalculatePoints(decimal subtotal)
        => throw new NotImplementedException();
}
