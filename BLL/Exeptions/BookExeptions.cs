
namespace BLL.Exceptions
{

    public class BookNotFoundException : Exception
    {
        public BookNotFoundException() : base("Book not found")
        {
        }

        public BookNotFoundException(string message) : base(message)
        {
        }

        public BookNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class BookDeleteException : Exception
    {
        public BookDeleteException() : base("Cannot delete book")
        {
        }

        public BookDeleteException(string message) : base(message)
        {
        }

        public BookDeleteException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class BookNotAvailableException : Exception
    {
        public BookNotAvailableException() : base("Book is not available")
        {
        }

        public BookNotAvailableException(string message) : base(message)
        {
        }

        public BookNotAvailableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}