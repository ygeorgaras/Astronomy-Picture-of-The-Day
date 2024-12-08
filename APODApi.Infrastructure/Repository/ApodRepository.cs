using APODApi.Core.Interfaces;
using APODApi.Core.Models;
using APODApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace APODApi.Infrastructure.Repository
{
    public class ApodRepository : IApodRepository
    {
        private readonly ApodDbContext _context;

        public ApodRepository(ApodDbContext context)
        {
            _context = context;
        }

        public async Task<ApodEntry?> GetByDateAsync(DateTime date)
        {
            return await _context.ApodEntries
                .FirstOrDefaultAsync(e => e.Date.Date == date.Date);
        }

        public async Task<IEnumerable<ApodEntry>> GetAllAsync()
        {
            return await _context.ApodEntries
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task AddAsync(ApodEntry entry)
        {
            await _context.ApodEntries.AddAsync(entry);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(DateTime date)
        {
            return await _context.ApodEntries
                .AnyAsync(e => e.Date.Date == date.Date);
        }
    }
}
