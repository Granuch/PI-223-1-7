using Moq;
using PI_223_1_7.Patterns.Repository;
using PI_223_1_7.Patterns.UnitOfWork;

namespace Tests.Mocks
{
    public class Mocks
    {
        public Mock<IUnitOfWork> UnitOfWorkMock { get; }
        public Mock<IOrderRepository> OrderRepositoryMock { get; }
        public Mock<IBookRepository> BookRepositoryMock { get; }

        public Mocks()
        {
            OrderRepositoryMock = new Mock<IOrderRepository>();
            BookRepositoryMock = new Mock<IBookRepository>();

            UnitOfWorkMock = new Mock<IUnitOfWork>();
            UnitOfWorkMock.Setup(u => u.orders).Returns(OrderRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.books).Returns(BookRepositoryMock.Object);
            UnitOfWorkMock.Setup(u => u.Complete()).Returns(Task.FromResult(1));
        }
    }
}