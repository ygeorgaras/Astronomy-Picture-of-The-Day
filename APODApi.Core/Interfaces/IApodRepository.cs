using APODApi.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APODApi.Core.Interfaces
{
    public interface IApodRepository
    {
        Task<ApodEntry?> GetByDateAsync(DateTime date);
        Task<IEnumerable<ApodEntry>> GetAllAsync();
        Task AddAsync(ApodEntry entry);
        Task<bool> ExistsAsync(DateTime date);
    }
}
