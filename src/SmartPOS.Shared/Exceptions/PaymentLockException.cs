namespace SmartPOS.Shared.Exceptions;

public class PaymentLockException : BusinessException
{
    public PaymentLockException() { }
    public PaymentLockException(string message) : base(message) { }
    public PaymentLockException(string message, Exception innerException) : base(message, innerException) { }
}

