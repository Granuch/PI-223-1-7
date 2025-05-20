using System;

namespace BLL.Exceptions
{

    public class OrderNotFoundException : Exception
    {
        public OrderNotFoundException() : base("Order not found")
        {
        }

        public OrderNotFoundException(string message) : base(message)
        {
        }

        public OrderNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class OrderStatusChangeException : Exception
    {
        public OrderStatusChangeException() : base("Cannot change order status")
        {
        }

        public OrderStatusChangeException(string message) : base(message)
        {
        }

        public OrderStatusChangeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class OrderDeleteException : Exception
    {
        public OrderDeleteException() : base("Cannot delete order")
        {
        }

        public OrderDeleteException(string message) : base(message)
        {
        }

        public OrderDeleteException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
