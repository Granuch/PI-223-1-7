using Mapping.DTOs;
using PI_223_1_7.Enums;
using PI_223_1_7.Models;
using System;
using System.Collections.Generic;

namespace Tests.TestHelpers
{
    public static class TestDataFactory
    {
        // Методи для створення Book
        public static Book CreateBook(int id = 1, bool isAvailable = true)
        {
            return new Book
            {
                Id = id,
                Name = $"Test Book {id}",
                Author = $"Test Author {id}",
                Description = $"Test Description {id}",
                Genre = GenreTypes.NonFiction,
                Type = BookTypes.Paper,
                IsAvaliable = isAvailable,
                Year = new DateTime(2022, 1, 1)
            };
        }

        public static BookDTO CreateBookDTO(int id = 1, bool isAvailable = true)
        {
            return new BookDTO
            {
                Id = id,
                Name = $"Test Book {id}",
                Author = $"Test Author {id}",
                Description = $"Test Description {id}",
                Genre = GenreTypes.NonFiction,
                Type = BookTypes.Paper,
                IsAvailable = isAvailable,
                Year = new DateTime(2022, 1, 1)
            };
        }

        // Методи для створення Order
        public static Order CreateOrder(int id = 1, int bookId = 1, string userId = "user1")
        {
            return new Order
            {
                Id = id,
                BookId = bookId,
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Type = OrderStatusTypes.Pending,
                Book = CreateBook(bookId)
            };
        }

        public static OrderDTO CreateOrderDTO(int id = 1, int bookId = 1, string userId = "user1")
        {
            return new OrderDTO
            {
                Id = id,
                BookId = bookId,
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Type = OrderStatusTypes.Pending,
                Book = CreateBookDTO(bookId)
            };
        }

        // Методи для створення списків
        public static List<Book> CreateBookList(int count = 5)
        {
            var books = new List<Book>();
            for (int i = 1; i <= count; i++)
            {
                books.Add(CreateBook(i));
            }
            return books;
        }

        public static List<Order> CreateOrderList(int count = 3)
        {
            var orders = new List<Order>();
            for (int i = 1; i <= count; i++)
            {
                orders.Add(CreateOrder(i, i));
            }
            return orders;
        }

        // Методи для створення User
        public static ApplicationUser CreateUser(string id = "user1", string email = "user1@example.com")
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                FirstName = $"User {id}",
                LastName = "Test",
                PhoneNumber = "1234567890",
                CreatedAt = DateTime.UtcNow
            };
        }

        // Методи для CreateUserRequest
        public static CreateUserRequest CreateUserRequest(string email = "newuser@example.com")
        {
            return new CreateUserRequest
            {
                Email = email,
                Password = "Password123!",
                FirstName = "New",
                LastName = "User",
                PhoneNumber = "1234567890",
                Role = "RegisteredUser"
            };
        }

        // Методи для UpdateUserRequest
        public static UpdateUserRequest CreateUpdateUserRequest()
        {
            return new UpdateUserRequest
            {
                FirstName = "Updated",
                LastName = "User",
                Email = "updated@example.com",
                PhoneNumber = "0987654321"
            };
        }
    }
}