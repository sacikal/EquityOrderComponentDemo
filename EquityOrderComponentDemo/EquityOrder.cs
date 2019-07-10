using System;
using System.Collections.Concurrent;

namespace EquityOrderComponentDemo
{
    public class EquityOrder : IEquityOrder
    {
        private readonly IOrderService _orderService;

        private static readonly ConcurrentDictionary<string, object> LockObjects = new ConcurrentDictionary<string, object>();
        private readonly OrderParameters _orderParameters;
        private bool _ignoreFurtherTicks;

        public EquityOrder(IOrderService orderService, OrderParameters orderParameters)
        {
            _orderService = orderService;
            _orderParameters = orderParameters;
        }

        public event OrderPlacedEventHandler OrderPlaced;
        public event OrderErroredEventHandler OrderErrored;

        public void ReceiveTick(string equityCode, decimal price)
        {
            var lockObject = LockObjects.GetOrAdd(equityCode, new object());

            lock (lockObject)
            {
                if (_ignoreFurtherTicks || price >= _orderParameters.PriceThreshold)
                {
                    return;
                }

                _ignoreFurtherTicks = true;

                try
                {
                    _orderService.Buy(equityCode, _orderParameters.Quantity, price);

                    OrderPlaced?.Invoke(new OrderPlacedEventArgs(equityCode, price));
                }
                catch (Exception ex)
                {
                    OrderErrored?.Invoke(new OrderErroredEventArgs(equityCode, price, ex));
                    _ignoreFurtherTicks = true;
                }
                finally
                {
                    LockObjects.TryRemove(equityCode, out _);
                }
            }
        }
    }
}