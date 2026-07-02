using Microsoft.EntityFrameworkCore;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;

namespace SmartPOS.Data.Repositories.Implementations;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer?> GetByPhoneAsync(string phone)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
    }

    public async Task<Customer?> GetByMemberCodeAsync(string memberCode)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.MemberCode == memberCode);
    }

    public async Task AddAsync(Customer customer)
    {
        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Customer>> GetCustomersQueryAsync(string? searchTerm, bool? isActive, bool? hasOrders, string? sortBy)
    {
        var query = _context.Customers.Include(c => c.Orders).AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (hasOrders.HasValue)
        {
            if (hasOrders.Value)
            {
                query = query.Where(c => c.Orders.Any());
            }
            else
            {
                query = query.Where(c => !c.Orders.Any());
            }
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(c => 
                (c.Phone != null && c.Phone.Contains(term)) ||
                (c.FullName != null && c.FullName.ToLower().Contains(term)) ||
                (c.Email != null && c.Email.ToLower().Contains(term)) ||
                c.Id.ToString().ToLower().Contains(term)
            );
        }

        query = sortBy switch
        {
            "NameAsc" => query.OrderBy(c => c.FullName),
            "NameDesc" => query.OrderByDescending(c => c.FullName),
            "IdAsc" => query.OrderBy(c => c.Id),
            "DateAsc" => query.OrderBy(c => c.CreatedAt),
            "DateDesc" => query.OrderByDescending(c => c.CreatedAt),
            "PointsAsc" => query.OrderBy(c => c.LoyaltyPoints),
            "PointsDesc" => query.OrderByDescending(c => c.LoyaltyPoints),
            "OrdersAsc" => query.OrderBy(c => c.Orders.Count),
            "OrdersDesc" => query.OrderByDescending(c => c.Orders.Count),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        return await query.ToListAsync();
    }
}

