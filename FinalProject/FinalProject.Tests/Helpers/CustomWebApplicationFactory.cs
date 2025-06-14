using FinalProject.API;
using FinalProject.Infrastructure.DbContexts;
using FinalProject.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FinalProject.Tests.Helpers
{
    public class CustomWebApplicationFactory : CustomWebApplicationFactory<Program>
    {
    }

    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registrations
                var appDbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                var authDbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));

                if (appDbContextDescriptor != null)
                    services.Remove(appDbContextDescriptor);

                if (authDbContextDescriptor != null)
                    services.Remove(authDbContextDescriptor);

                // Register in-memory databases for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationTestDb");
                });

                services.AddDbContext<AuthDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryAuthDb");
                });

                // Build the service provider
                var serviceProvider = services.BuildServiceProvider();

                // Create and initialize db contexts
                using (var scope = serviceProvider.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var appDb = scopedServices.GetRequiredService<ApplicationDbContext>();
                    var authDb = scopedServices.GetRequiredService<AuthDbContext>();
                    var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                    appDb.Database.EnsureCreated();
                    authDb.Database.EnsureCreated();

                    try
                    {
                        // Seed the databases
                        SeedApplicationDb(appDb);
                        SeedAuthDb(authDb);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred seeding the database with test data. Error: {Message}", ex.Message);
                    }
                }
            });
        }

        private void SeedApplicationDb(ApplicationDbContext context)
        {
            // Clear existing data
            context.Notifications.RemoveRange(context.Notifications);
            context.Fines.RemoveRange(context.Fines);
            context.BorrowingTransactions.RemoveRange(context.BorrowingTransactions);
            context.Books.RemoveRange(context.Books);
            context.Members.RemoveRange(context.Members);
            context.SaveChanges();

            // Seed Members
            var members = new List<Member>
            {
                new Member {
                    MemberID = 1,
                    Name = "John Doe",
                    Email = "john@example.com",
                    Phone = "1234567890",
                    Address = "123 Main St",
                    MembershipStatus = "Active"
                },
                new Member {
                    MemberID = 2,
                    Name = "Jane Smith",
                    Email = "jane@example.com",
                    Phone = "0987654321",
                    Address = "456 Oak St",
                    MembershipStatus = "Active"
                }
            };
            context.Members.AddRange(members);

            // Seed Books
            var books = new List<Book>
            {
                new Book {
                    BookID = 1,
                    Title = "The Great Gatsby",
                    Author = "F. Scott Fitzgerald",
                    Genre = "Classic",
                    ISBN = "9780743273565",
                    YearPublished = 1925,
                    AvailableCopies = 5
                },
                new Book {
                    BookID = 2,
                    Title = "To Kill a Mockingbird",
                    Author = "Harper Lee",
                    Genre = "Fiction",
                    ISBN = "9780061120084",
                    YearPublished = 1960,
                    AvailableCopies = 3
                }
            };
            context.Books.AddRange(books);

            // Seed BorrowingTransactions
            var transactions = new List<BorrowingTransaction>
            {
                new BorrowingTransaction {
                    TransactionID = 1,
                    BookID = 1,
                    MemberID = 1,
                    BorrowDate = DateTime.Now.AddDays(-10),
                    ReturnDate = DateTime.Now.AddDays(3), // due in 3 days
                    Status = "Borrowed",
                    Book = books[0],
                    Member = members[0]
                },
                new BorrowingTransaction {
                    TransactionID = 2,
                    BookID = 2,
                    MemberID = 2,
                    BorrowDate = DateTime.Now.AddDays(-15),
                    ReturnDate = DateTime.Now.AddDays(-2), // overdue by 2 days
                    Status = "Borrowed",
                    Book = books[1],
                    Member = members[1]
                },
                new BorrowingTransaction {
                    TransactionID = 3,
                    BookID = 1,
                    MemberID = 2,
                    BorrowDate = DateTime.Now.AddDays(-45),
                    ReturnDate = DateTime.Now.AddDays(-35), // Overdue by more than 30 days
                    Status = "Borrowed"
                }
            };
            context.BorrowingTransactions.AddRange(transactions);

            // Seed Fines
            var fines = new List<Fine>
            {
                new Fine {
                    FineID = 1,
                    MemberID = 1,
                    Amount = 10.50m,
                    Status = "Pending",
                    TransactionDate = DateTime.Now.AddDays(-10),
                    Member = members[0]
                },
                new Fine {
                    FineID = 2,
                    MemberID = 2,
                    Amount = 5.25m,
                    Status = "Paid",
                    TransactionDate = DateTime.Now.AddDays(-5),
                    Member = members[1]
                }
            };
            context.Fines.AddRange(fines);

            // Seed Notifications
            var notifications = new List<Notification>
            {
                new Notification {
                    NotificationID = 1,
                    MemberID = 1,
                    Message = "Your book is due in 3 days.",
                    DateSent = DateTime.Now.AddDays(-1),
                    Member = members[0]
                },
                new Notification {
                    NotificationID = 2,
                    MemberID = 2,
                    Message = "Your book is overdue by 2 days.",
                    DateSent = DateTime.Now,
                    Member = members[1]
                }
            };
            context.Notifications.AddRange(notifications);

            context.SaveChanges();
        }

        private void SeedAuthDb(AuthDbContext context)
        {
            context.Users.Add(new Microsoft.AspNetCore.Identity.IdentityUser
            {
                Id = "1",
                UserName = "testuser@library.com",
                Email = "testuser@library.com",
                EmailConfirmed = true
            });

            context.SaveChanges();
        }
    }
}
