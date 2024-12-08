using APODApi.Core.Models;
using APODApi.Infrastructure.Data;
using APODApi.Infrastructure.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APODApi.Tests.Repositories
{
    public class ApodRepositoryTests : IDisposable
    {
        private readonly ApodDbContext _context;
        private readonly ApodRepository _repository;

        public ApodRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApodDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApodDbContext(options);
            _repository = new ApodRepository(_context);
        }

        [Fact]
        public async Task AddAsync_AddsEntryToDatabase()
        {
            // Arrange
            var entry = new ApodEntry
            {
                Title = "Test Entry",
                Date = DateTime.Today,
                Url = "https://example.com",
                MediaType = "image"
            };

            // Act
            await _repository.AddAsync(entry);

            // Assert
            var savedEntry = await _context.ApodEntries.FirstOrDefaultAsync();
            savedEntry.Should().NotBeNull();
            savedEntry.Should().BeEquivalentTo(entry, opts =>
                opts.Excluding(e => e.Id));
        }

        [Fact]
        public async Task GetByDateAsync_ReturnsCorrectEntry()
        {
            // Arrange
            var entry = new ApodEntry
            {
                Title = "Test Entry",
                Date = DateTime.Today,
                Url = "https://example.com",
                MediaType = "image"
            };
            await _context.ApodEntries.AddAsync(entry);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByDateAsync(DateTime.Today);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(entry);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
