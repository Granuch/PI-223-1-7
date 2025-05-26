using AutoMapper;
using BLL.Services;
using Tests.TestHelpers;
using Mapping.DTOs;
using Moq;
using PI_223_1_7.Models;

namespace Tests.Services
{
    [TestFixture]
    public class OrderServiceTests
    {
        private Mocks.Mocks _mockUnitOfWork;
        private IMapper _mapper;
        private OrderService _orderService;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mocks.Mocks();
            _mapper = MapperHelper.CreateMapper();
            _orderService = new OrderService(_mockUnitOfWork.UnitOfWorkMock.Object, _mapper);
        }

        [Test]
        public async Task GetSpecificOrder_ExistingId_ReturnsOrder()
        {
            // Arrange
            var orderId = 1;
            var order = TestDataFactory.CreateOrder(orderId);
            _mockUnitOfWork.OrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            // Act
            var result = await _orderService.GetSpecificOrder(orderId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(orderId));
        }

        [Test]
        public async Task CreateOrder_ValidOrder_ReturnsCreatedOrder()
        {
            // Arrange
            var orderDto = TestDataFactory.CreateOrderDTO();

            // Act
            var result = await _orderService.CreateOrder(orderDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(orderDto.Id));
            _mockUnitOfWork.OrderRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Order>()), Times.Once);
            _mockUnitOfWork.UnitOfWorkMock.Verify(uow => uow.Complete(), Times.Once);
        }

        [Test]
        public void CreateOrder_NullOrder_ThrowsArgumentNullException()
        {
            // Arrange
            OrderDTO nullOrder = null;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _orderService.CreateOrder(nullOrder));
        }

        [Test]
        public async Task GetAllWithDetails_ReturnsOrdersWithDetails()
        {
            // Arrange
            var orders = TestDataFactory.CreateOrderList();
            _mockUnitOfWork.OrderRepositoryMock
                .Setup(repo => repo.GetAllWithDetailsAsync())
                .ReturnsAsync(orders);

            // Act
            var result = await _orderService.GetAllWithDetails();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(orders.Count));
        }

        [Test]
        public async Task GetAllWithoutDetails_ReturnsOrdersWithoutDetails()
        {
            // Arrange
            var orders = TestDataFactory.CreateOrderList();
            _mockUnitOfWork.OrderRepositoryMock
                .Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(orders);

            // Act
            var result = await _orderService.GetAllWithoutDetails();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(orders.Count));
        }

        [Test]
        public async Task DeleteOrderById_ExistingId_DeletesOrder()
        {
            // Arrange
            var orderId = 1;
            var order = TestDataFactory.CreateOrder(orderId);
            _mockUnitOfWork.OrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            // Act
            await _orderService.DeleteOrderById(orderId);

            // Assert
            _mockUnitOfWork.OrderRepositoryMock.Verify(repo => repo.Delete(order), Times.Once);
            _mockUnitOfWork.OrderRepositoryMock.Verify(repo => repo.SaveAsync(), Times.Once);
        }

        [Test]
        public void DeleteOrderById_NonExistingId_ThrowsArgumentException()
        {
            // Arrange
            var nonExistingId = 999;
            _mockUnitOfWork.OrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(nonExistingId))
                .ReturnsAsync((Order)null);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _orderService.DeleteOrderById(nonExistingId));
        }

        [Test]
        public async Task UpdateOrder_ExistingOrder_UpdatesOrder()
        {
            // Arrange
            var orderId = 1;
            var existingOrder = TestDataFactory.CreateOrder(orderId);
            var updatedOrderDto = TestDataFactory.CreateOrderDTO(orderId);
            updatedOrderDto.Type = PI_223_1_7.Enums.OrderStatusTypes.Completed;

            _mockUnitOfWork.OrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(orderId))
                .ReturnsAsync(existingOrder);

            // Act
            await _orderService.UpdateOrder(updatedOrderDto);

            // Assert
            _mockUnitOfWork.OrderRepositoryMock.Verify(repo => repo.Update(It.IsAny<Order>()), Times.Once);
            _mockUnitOfWork.UnitOfWorkMock.Verify(uow => uow.Complete(), Times.Once);
        }

        [Test]
        public void UpdateOrder_NullOrder_ThrowsKeyNotFoundException()
        {
            // Arrange
            OrderDTO nullOrder = null;

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _orderService.UpdateOrder(nullOrder));
        }
    }
}