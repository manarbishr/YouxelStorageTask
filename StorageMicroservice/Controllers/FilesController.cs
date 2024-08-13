using Microsoft.AspNetCore.Mvc;
using StorageMicroservice.Services;

namespace StorageMicroservice.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IStorageService _storageService;

        public FilesController(IStorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty.");
            }

            var result = await _storageService.UploadFileAsync(file);
            return Ok(result);
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> GetFile(Guid id)
        {
            var (content, file) = await _storageService.GetFileAsync(id);
            return File(content, file.ContentType, file.Name);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteFile(Guid id)
        {
            await _storageService.DeleteFileAsync(id);
            return NoContent();
        }
    }
}
