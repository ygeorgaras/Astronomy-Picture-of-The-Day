using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APODApi.Core.Interfaces
{
    public interface IWallpaperService
    {
        Task<string> SetWallpaperFromUrlAsync(string imageUrl, string title);
        Task SetWallpaperFromLocalPathAsync(string localPath);
        Task<string> DownloadImageAsync(string imageUrl, string title);
    }
}
