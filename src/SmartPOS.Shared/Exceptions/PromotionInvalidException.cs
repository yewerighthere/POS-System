namespace SmartPOS.Shared.Exceptions;

public class PromotionInvalidException : BusinessException
{
    public PromotionInvalidException() { }
    public PromotionInvalidException(string message) : base(message) { }
    public PromotionInvalidException(string message, Exception innerException) : base(message, innerException) { }
}

