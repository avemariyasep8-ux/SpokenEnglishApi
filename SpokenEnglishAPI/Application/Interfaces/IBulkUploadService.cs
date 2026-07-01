using Microsoft.AspNetCore.Http;

namespace SpokenEnglishAPI.Application.Interfaces
{
    public interface IBulkUploadService
    {
        Task<byte[]> DownloadTemplate();
        Task<string> UploadBulkData(IFormFile file);
    }
}
