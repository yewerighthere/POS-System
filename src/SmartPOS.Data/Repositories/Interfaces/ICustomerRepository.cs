using SmartPOS.Data.Entities;

namespace SmartPOS.Data.Repositories.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id); Task<Customer?> GetByPhoneAsync(string phone); Task<Customer?> GetByMemberCodeAsync(string memberCode); Task AddAsync(Customer customer); Task UpdateAsync(Customer customer);
}


