namespace Tgs.Payment.UBPay;

public class ApiFailException : Exception
{
    public ApiFailException()
    {
    }

    public ApiFailException(string? message) : base(message)
    {
    }
}