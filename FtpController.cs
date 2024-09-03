using Microsoft.AspNetCore.Mvc;
using FileUploadApi.Services;
using System.Threading.Tasks;

namespace FileUploadApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FileUploadController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] string localPath, [FromForm] string remotePath)
        {

            var result = await _fileService.UploadFileAsync(localPath, remotePath);
            return Ok(result);
        }

        [HttpPost("replace")]
        public async Task<IActionResult> ReplaceFile([FromForm] string localPath, [FromForm] string remotePath)
        {
            var result = await _fileService.ReplaceFileAsync(localPath, remotePath);
            return Ok(result);
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteFile([FromForm] string remotePath)
        {
            var result = await _fileService.DeleteFileAsync(remotePath);
            return Ok(result);
        }
    }
}
