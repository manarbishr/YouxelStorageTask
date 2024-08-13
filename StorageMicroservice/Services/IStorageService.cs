using StorageMicroservice.Models;

namespace StorageMicroservice.Services
{
	public interface IStorageService
	{
		Task<FileModel> UploadFileAsync(IFormFile file);
		Task<(Stream content, FileModel fileInfo)> GetFileAsync(Guid id);

        Task DeleteFileAsync(Guid id);
	}
}
