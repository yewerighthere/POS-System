using Microsoft.Extensions.Logging;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Customer;
using SmartPOS.Shared.Exceptions;

namespace SmartPOS.Services.Implementations;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ICustomerRepository customerRepository, IOrderRepository orderRepository, ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
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

    public async Task AddLoyaltyPointsAsync(Guid customerId, int points)
    {
        if (points <= 0) return;
        var customer = await _customerRepository.GetByIdAsync(customerId).ConfigureAwait(false);
        if (customer != null)
        {
            customer.LoyaltyPoints += points;
            await _customerRepository.UpdateAsync(customer).ConfigureAwait(false);
        }
    }

    public async Task DeductLoyaltyPointsAsync(Guid customerId, int points)
    {
        if (points <= 0) return;
        var customer = await _customerRepository.GetByIdAsync(customerId).ConfigureAwait(false);
        if (customer != null)
        {
            customer.LoyaltyPoints -= points;
            if (customer.LoyaltyPoints < 0) customer.LoyaltyPoints = 0;
            await _customerRepository.UpdateAsync(customer).ConfigureAwait(false);
        }
    }

    public int CalculatePoints(decimal subtotal)
    {
        if (subtotal <= 0) return 0;
        return (int)Math.Floor(subtotal / 10000m);
    }

    public async Task<IEnumerable<CustomerListDto>> GetCustomerListAsync(string? searchTerm, string? statusFilter, string? orderFilter, string? sortOption)
    {
        bool? isActive = statusFilter switch
        {
            "Active" => true,
            "Banned" => false,
            _ => null
        };

        bool? hasOrders = orderFilter switch
        {
            "Has Orders" => true,
            "No Orders" => false,
            _ => null
        };

        var customers = await _customerRepository.GetCustomersQueryAsync(searchTerm, isActive, hasOrders, sortOption).ConfigureAwait(false);

        return customers.Select(c => new CustomerListDto
        {
            Id = c.Id,
            FullName = c.FullName,
            Phone = c.Phone,
            Email = c.Email,
            OrderCount = c.Orders?.Count ?? 0,
            LoyaltyPoints = c.LoyaltyPoints,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        });
    }

    public async Task ToggleCustomerStatusAsync(Guid customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId).ConfigureAwait(false);
        if (customer != null)
        {
            customer.IsActive = !customer.IsActive;
            await _customerRepository.UpdateAsync(customer).ConfigureAwait(false);
        }
    }

    public async Task<CustomerDetailDto?> GetCustomerDetailAsync(Guid customerId)
    {
        var customers = await _customerRepository.GetCustomersQueryAsync(null, null, null, null).ConfigureAwait(false);
        var customer = customers.FirstOrDefault(c => c.Id == customerId);
        
        if (customer == null) return null;

        return new CustomerDetailDto
        {
            Id = customer.Id,
            FullName = customer.FullName ?? string.Empty,
            Phone = customer.Phone ?? string.Empty,
            Email = customer.Email,
            LoyaltyPoints = customer.LoyaltyPoints,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt,
            Orders = customer.Orders.OrderByDescending(o => o.CreatedAt).Select(o => new CustomerOrderDto
            {
                Id = o.Id,
                CreatedAt = o.CreatedAt,
                TotalAmount = o.TotalAmount
            }).ToList()
        };
    }

    public async Task<CustomerDetailDto> UpdateCustomerAsync(UpdateCustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new BusinessException("Tên khách hàng không được để trống");
        if (string.IsNullOrWhiteSpace(dto.Phone))
            throw new BusinessException("Số điện thoại không được để trống");

        var customer = await _customerRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (customer == null)
            throw new BusinessException("Khách hàng không tồn tại");

        customer.FullName = dto.FullName;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.IsActive = dto.IsActive;

        await _customerRepository.UpdateAsync(customer).ConfigureAwait(false);
        
        return await GetCustomerDetailAsync(customer.Id) ?? throw new BusinessException("Lỗi sau khi cập nhật");
    }

    public async Task<CustomerOrderDetailDto?> GetCustomerOrderDetailAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(orderId).ConfigureAwait(false);
        if (order == null) return null;

        var lastPayment = order.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();

        return new CustomerOrderDetailDto
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            PointsEarned = order.PointsEarned,
            PointsUsed = order.PointsUsed,
            PointsDiscountAmount = order.PointsDiscountAmount,
            PaymentMethod = lastPayment?.PaymentMethod.ToString() ?? order.PaymentMethod?.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            AmountReceived = lastPayment?.AmountReceived ?? 0,
            ChangeAmount = lastPayment?.ChangeAmount ?? 0,
            Items = order.Items.Select(i => new CustomerOrderItemDto
            {
                Id = i.Id,
                ProductName = i.ProductName,
                Sku = i.Sku,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                DiscountAmount = i.DiscountAmount,
                Subtotal = i.Subtotal
            }).ToList()
        };
    }
}
