namespace EquityOrderComponentDemo
{
    public interface IEquityOrder : IOrderPlaced, IOrderErrored
    {
        void ReceiveTick(string equityCode, decimal price);
    }
}