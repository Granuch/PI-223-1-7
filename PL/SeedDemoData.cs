using BLL.Interfaces;
using BLL.Services;
using Mapping.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PI_223_1_7.DbContext;
using PI_223_1_7.Enums;
using PI_223_1_7.Models;
using PI_223_1_7.Patterns.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public class SeedDemoData
{
    public static async Task SeedData(IServiceProvider serviceProvider)
    {
        try
        {
            Console.WriteLine("Starting database seeding...");

            // Try direct database approach if services aren't working properly
            var unitOfWork = serviceProvider.GetService<IUnitOfWork>();

            // Try to get mapper
            var mapper = serviceProvider.GetService<AutoMapper.IMapper>();
            if (mapper == null)
            {
                Console.WriteLine("AutoMapper not available. Cannot seed data properly.");
                return;
            }

            // Create book service
            var bookService = new BookService(unitOfWork, mapper);

            // Try to get Identity services if available
            try
            {
                var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();
                var roleManager = serviceProvider.GetService<RoleManager<ApplicationRole>>();

                // Only seed users and roles if Identity is configured
                if (userManager != null && roleManager != null)
                {
                    Console.WriteLine("Seeding users and roles...");
                    // Initialize roles and admin user
                    await PL.Controllers.RoleInitializer.InitializeAsync(userManager, roleManager);

                    // Create additional demo users
                    await CreateDemoUsers(userManager);
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue with book seeding
                Console.WriteLine($"Identity services not available. Skipping user and role seeding. Error: {ex.Message}");
            }

            Console.WriteLine("Seeding books...");
            var books = await SeedBooks(bookService);

            if (books == null || books.Count == 0)
            {
                IEnumerable<BookDTO> bookss = await bookService.GetAllBooksAsync();
                Console.WriteLine("Seeding orders...");
                await SeedOrders(unitOfWork, bookss.ToList());
            }
            else
            {
                Console.WriteLine("Seeding orders...");
                await SeedOrders(unitOfWork, books);
            }
          
            Console.WriteLine("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while seeding demo data: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            throw; // Re-throw to allow calling code to handle the error
        }
    }

    private static async Task CreateDemoUsers(UserManager<ApplicationUser> userManager)
    {
        var existingUsers = userManager.Users.Where(u => u.Email != "admin@example.com");
        if (existingUsers.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Demo users already exist. Skipping user seeding.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("No demo users found. Creating demo users...");
        Console.ResetColor();

        // Create a manager user
        string managerEmail = "manager@example.com";
        if (await userManager.FindByNameAsync(managerEmail) == null)
        {
            ApplicationUser manager = new ApplicationUser
            {
                Email = managerEmail,
                UserName = managerEmail,
                FirstName = "Manager",
                LastName = "User",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(manager, "Manager123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(manager, "Manager");
            }
        }

        // Create regular users
        var regularUsers = new List<(string email, string password, string firstName, string lastName)>
        {
            ("user1@example.com", "User123!", "John", "Doe"),
            ("user2@example.com", "User123!", "Jane", "Smith"),
            ("user3@example.com", "User123!", "Michael", "Johnson")
        };

        foreach (var user in regularUsers)
        {
            if (await userManager.FindByNameAsync(user.email) == null)
            {
                ApplicationUser appUser = new ApplicationUser
                {
                    Email = user.email,
                    UserName = user.email,
                    FirstName = user.firstName,
                    LastName = user.lastName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(appUser, user.password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(appUser, "RegisteredUser");
                }
            }
        }
    }

    private static async Task<List<BookDTO>> SeedBooks(IBookService bookService)
    {
        var existingBooks = await bookService.GetAllBooksAsync();
        if (existingBooks != null && existingBooks.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Books already exist in the database. Skipping seeding books.");
            Console.ResetColor();
            return new List<BookDTO>();
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("No books found. Starting seeding...");
        Console.ResetColor();

        var demoBooks = new List<BookDTO>
        {
            new BookDTO
            {
                Name = "Pride and Prejudice",
                Author = "Jane Austen",
                Description = "A classic novel about marriage and social status in early 19th-century England.",
                Genre = GenreTypes.Western,
                Type = BookTypes.Paper,
                IsAvailable = true,
                Year = new DateTime(1813, 1, 28)
            },
            new BookDTO
            {
                Name = "1984",
                Author = "George Orwell",
                Description = "A dystopian novel set in a totalitarian society.",
                Genre = GenreTypes.ScienceFiction,
                Type = BookTypes.Audio,
                IsAvailable = true,
                Year = new DateTime(1949, 6, 8)
            },
            new BookDTO
            {
                Name = "To Kill a Mockingbird",
                Author = "Harper Lee",
                Description = "A novel about racial injustice and moral growth set in the American South.",
                 Genre = GenreTypes.Romance,
                Type = BookTypes.Ebook,
                IsAvailable = true,
                Year = new DateTime(1960, 7, 11)
            },
            new BookDTO
            {
                Name = "The Great Gatsby",
                Author = "F. Scott Fitzgerald",
                Description = "A novel about the American Dream set during the Roaring Twenties.",
                Genre = GenreTypes.Western,
                Type = BookTypes.Paper,
                IsAvailable = true,
                Year = new DateTime(1925, 4, 10)
            },
            new BookDTO
            {
                Name = "Sapiens: A Brief History of Humankind",
                Author = "Yuval Noah Harari",
                Description = "A book exploring the history and impact of Homo sapiens.",
                Genre = GenreTypes.ScienceFiction,
                Type = BookTypes.Audio,
                IsAvailable = true,
                Year = new DateTime(2011, 1, 1)
            }
        };

        var addedBooks = new List<BookDTO>();

        foreach (var bookDto in demoBooks)
        {
            try
            {
                // Fix potential property naming inconsistency
                // Some DTOs might use IsAvailable while the model uses IsAvaliable
                var propertyInfo = typeof(BookDTO).GetProperty("IsAvailable");
                if (propertyInfo != null)
                {
                    // Set the property that exists
                    propertyInfo.SetValue(bookDto, true);
                }

                Console.WriteLine($"Attempting to add book: {bookDto.Name}");
                var addedBook = await bookService.AddBookAsync(bookDto);
                addedBooks.Add(addedBook);
                Console.WriteLine($"Successfully added book: {bookDto.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding book {bookDto.Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                // Continue trying to add other books
                continue;
            }
        }

        Console.WriteLine($"Total books added: {addedBooks.Count}");
        return addedBooks;
    }

    private static async Task SeedOrders(IUnitOfWork unitOfWork, List<BookDTO> books)
    {
        var existingOrders = await unitOfWork.orders.GetAllAsync();
        if (existingOrders != null && existingOrders.Any())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Orders already exist in the database. Skipping seeding order.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("No orders found. Starting seeding...");
        Console.ResetColor();

        // Create some orders
        var random = new Random();

        // Take a few books to be ordered
        var booksToOrder = books.Take(3).ToList();

        // Create default user IDs if we don't have actual users
        List<int> userIds = new List<int> { 1, 2, 3 };

        try
        {
            // Try to get actual users if available
            var users = await unitOfWork.users.FindAsync(u => u.Email != "admin@example.com");
            var usersList = users.ToList();

            if (usersList.Any())
            {
                userIds = usersList.Select(u => int.Parse(u.Id)).ToList();
            }
        }
        catch
        {
            // Continue with default user IDs
        }

        foreach (var book in booksToOrder)
        {
            // Select a random user ID
            var userId = userIds[random.Next(userIds.Count)];

            // Create an order
            var order = new Order
            {
                BookId = book.Id,
                UserId = userId,
                OrderDate = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                Type = OrderStatusTypes.Approved
            };

            // Update book availability
            var bookEntity = await unitOfWork.books.GetByIdAsync(book.Id);
            if (bookEntity != null)
            {
                bookEntity.IsAvaliable = false;
                unitOfWork.books.Update(bookEntity);
            }

            // Add the order
            await unitOfWork.orders.AddAsync(order);
        }

        // Save all changes
        await unitOfWork.books.SaveAsync();
    }
}