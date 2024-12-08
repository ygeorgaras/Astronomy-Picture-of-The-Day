using APODApi.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APODApi.Core.Interfaces
{
    public interface INasaApodService
    {
        Task<NasaApodResponse> GetLatestApodAsync();
        Task<NasaApodResponse> GetApodByDateAsync(string date);
    }
}
