using Moq;
using NUnit.Framework;
using System;
using FluentAssertions;

namespace EquityOrderComponentDemo.Tests
{
    public class EquityOrderTests
    {
        private Mock<IOrderService> _orderService;
        private IEquityOrder _equityOrder;
        private OrderParameters _orderParameters;
        private int quantity = 50;
        private decimal priceThreshold = 5.00M;
        private OrderPlacedEventArgs _orderPlacedEventArgs;
        private OrderErroredEventArgs _orderErroredEventArgs;

        [SetUp]
        public void Setup()
        {
            _orderService = new Mock<IOrderService>(MockBehavior.Strict);
            _orderPlacedEventArgs = null;
            _orderErroredEventArgs = null;
            _orderParameters = new OrderParameters { PriceThreshold = priceThreshold, Quantity = quantity };
            _equityOrder = new EquityOrder(_orderService.Object, _orderParameters);
        }

        [Test]
        public void GivenPriceBelowPriceThreshold_WhenReceiveTickCalled_ThenBuysOnlyOnce()
        {
            // Arrange
            var equityCode = "AMZN";
            decimal stockPrice = 3.00M;

            _equityOrder.OrderPlaced += orderPlacedArgs => _orderPlacedEventArgs = orderPlacedArgs;
            _equityOrder.OrderErrored += orderErroredArgs => _orderErroredEventArgs = orderErroredArgs;

            _orderService.Setup(x => x.Buy(equityCode, _orderParameters.Quantity, stockPrice));

            // Act
            _equityOrder.ReceiveTick(equityCode, stockPrice);
            _equityOrder.ReceiveTick(equityCode, stockPrice);

            // Assert
            _orderPlacedEventArgs.Should().NotBeNull();
            _orderPlacedEventArgs.EquityCode.Should().Be(equityCode);
            _orderPlacedEventArgs.Price.Should().Be(stockPrice);

            _orderErroredEventArgs.Should().BeNull();

            _orderService.Verify(x => x.Buy(equityCode, _orderParameters.Quantity, stockPrice), Times.Once);
        }

        [Test]
        public void GivenMultipleDifferentPrices_WhenReceiveTickCalled_ThenBuyCorrectStockPriceOnlyOnce()
        {
            // Arrange
            var equityCode = "AMZN";
            decimal firstStockPrice = 3.00M;
            decimal secondStockPrice = 4.00M;
            decimal thirdStockPrice = 4.50M;

            _equityOrder.OrderPlaced += orderPlacedArgs => _orderPlacedEventArgs = orderPlacedArgs;
            _equityOrder.OrderErrored += orderErroredArgs => _orderErroredEventArgs = orderErroredArgs;

            _orderService.Setup(x => x.Buy(equityCode, _orderParameters.Quantity, firstStockPrice));

            // Act
            _equityOrder.ReceiveTick(equityCode, firstStockPrice);
            _equityOrder.ReceiveTick(equityCode, secondStockPrice);
            _equityOrder.ReceiveTick(equityCode, thirdStockPrice);

            // Assert
            _orderPlacedEventArgs.Should().NotBeNull();
            _orderPlacedEventArgs.EquityCode.Should().Be(equityCode);
            _orderPlacedEventArgs.Price.Should().Be(firstStockPrice);

            _orderErroredEventArgs.Should().BeNull();

            _orderService.Verify(x => x.Buy(equityCode, _orderParameters.Quantity, firstStockPrice), Times.Once);
        }
        [Test]
        public void GivenPriceAbovePriceThreshold_WhenReceiveTickCalled_ThenNotBuyOrSell()
        {
            // Arrange
            var equityCode = "AMZN";
            decimal stockPrice = 6.00M;

            _equityOrder.OrderPlaced += orderPlacedArgs => _orderPlacedEventArgs = orderPlacedArgs;
            _equityOrder.OrderErrored += orderErroredArgs => _orderErroredEventArgs = orderErroredArgs;

            // Act
            _equityOrder.ReceiveTick(equityCode, stockPrice);

            // Assert
            _orderPlacedEventArgs.Should().BeNull();
            _orderErroredEventArgs.Should().BeNull();
        }

        [Test]
        public void GivenPriceEqualsPriceThreshold_WhenReceiveTickCalled_ThenNotBuyOrSell()
        {
            // Arrange
            var equityCode = "AMZN";
            decimal stockPrice = 5.00M;

            // Act
            _equityOrder.ReceiveTick(equityCode, stockPrice);

            // Assert
            _equityOrder.OrderPlaced += orderPlacedArgs => _orderPlacedEventArgs = orderPlacedArgs;
            _equityOrder.OrderErrored += orderErroredArgs => _orderErroredEventArgs = orderErroredArgs;

            _orderPlacedEventArgs.Should().BeNull();
            _orderErroredEventArgs.Should().BeNull();
        }

        [Test]
        public void GivenPriceBelowPriceThreshold_WhenReceiveTickCalled_ThenReturnsError()
        {
            // Arrange
            var equityCode = "AMZN";
            decimal stockPrice = 4.00M;
            var argumentException = new ArgumentException("Something is wrong with the argument.");

            _equityOrder.OrderPlaced += orderPlacedArgs => _orderPlacedEventArgs = orderPlacedArgs;
            _equityOrder.OrderErrored += orderErroredArgs => _orderErroredEventArgs = orderErroredArgs;

            _orderService.Setup(x => x.Buy(equityCode, _orderParameters.Quantity, stockPrice)).Throws(argumentException);

            // Act
            _equityOrder.ReceiveTick(equityCode, stockPrice);

            // Assert
            _orderPlacedEventArgs.Should().BeNull();

            _orderErroredEventArgs.Should().NotBeNull();
            _orderErroredEventArgs.GetException().Should().Be(argumentException);

            _orderErroredEventArgs.EquityCode.Should().Be(equityCode);
            _orderErroredEventArgs.Price.Should().Be(stockPrice);
        }
    }
}