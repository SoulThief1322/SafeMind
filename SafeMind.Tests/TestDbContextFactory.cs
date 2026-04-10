using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SafeMind.Data;
using System;

namespace SafeMind.Tests
{
    public static class TestDbContextFactory
    {
        public static SafeMindDbContext Create(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<SafeMindDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context = new SafeMindDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }
    }
}
