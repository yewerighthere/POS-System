namespace SmartPOS.Shared.Exceptions;

public class StockInsufficientException : BusinessException
{
    public StockInsufficientException() { }
    public StockInsufficientException(string message) : base(message) { }
    public StockInsufficientException(string message, Exception innerException) : base(message, innerException) { }
}

