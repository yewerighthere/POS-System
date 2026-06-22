using System;
using System.Threading.Tasks;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Customer;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Data.Entities;

namespace SmartPOS.Services.Implementations;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto?> FindByPhoneAsync(string phone)
    {
        // Simple mock implementation for now to make it build
        var customer = await _customerRepository.GetByPhoneAsync(phone);
        if (customer == null) return null;
        
        return new CustomerDto
        {
            Id = customer.Id,
            Phone = customer.Phone,
            MemberCode = customer.MemberCode ?? string.Empty
        };
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        var newCustomer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            MemberCode = dto.Phone // fallback
        };
        await _customerRepository.AddAsync(newCustomer);
        
        return new CustomerDto
        {
            Id = newCustomer.Id,
            FullName = newCustomer.FullName,
            Phone = newCustomer.Phone,
            MemberCode = newCustomer.MemberCode
        };
    }

    public async Task AddLoyaltyPointsAsync(Guid customerId, int points)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer != null)
        {
            customer.LoyaltyPoints += points;
            await _customerRepository.UpdateAsync(customer);
        }
    }

    public int CalculatePoints(decimal subtotal)
    {
        return (int)(subtotal / 100);
    }
}
