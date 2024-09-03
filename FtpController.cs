using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FileUploadApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly string _ftpServerAddress = "ftp://localhost";
        private readonly string _ftpUsername = "user";
        private readonly string _ftpPassword = "pass";
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] string localPath, [FromForm] string remotePath)
        {
            if (string.IsNullOrEmpty(localPath) || string.IsNullOrEmpty(remotePath))
            {
                return BadRequest("Local path and remote path are required.");
            }

            if (!System.IO.File.Exists(localPath))
            {
                return NotFound("Local file not found.");
            }

            var ftpUri = new Uri($"{_ftpServerAddress}/{remotePath.TrimStart('/')}/{Path.GetFileName(localPath)}");
            var request = (FtpWebRequest)WebRequest.Create(ftpUri);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);

            byte[] fileContents = await System.IO.File.ReadAllBytesAsync(localPath);
            request.ContentLength = fileContents.Length;

            using (var requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(fileContents, 0, fileContents.Length);
            }

            using (var response = (FtpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode == FtpStatusCode.ClosingData)
                {
                    return Ok("File uploaded successfully.");
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Error uploading file: {response.StatusDescription}");
                }
            }
        }

        [HttpPost("replace")]
        public async Task<IActionResult> ReplaceFile([FromForm] string localPath, [FromForm] string remotePath)
        {
            if (string.IsNullOrEmpty(localPath) || string.IsNullOrEmpty(remotePath))
            {
                return BadRequest("Local path and remote path are required.");
            }

            if (!System.IO.File.Exists(localPath))
            {
                return NotFound("Local file not found.");
            }

            var fileName = Path.GetFileName(localPath);
            var remoteDirectory = remotePath.TrimEnd('/');
            var ftpBaseUri = new Uri($"{_ftpServerAddress}/{remoteDirectory}");
            var fileList = await ListFilesInDirectoryAsync(ftpBaseUri);

            foreach (var file in fileList)
            {
                if (!file.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    var fileUri = new Uri(ftpBaseUri, file);
                    var deleteRequest = (FtpWebRequest)WebRequest.Create(fileUri);
                    deleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                    deleteRequest.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);
                    using (var deleteResponse = (FtpWebResponse)await deleteRequest.GetResponseAsync())
                    {
                        if (deleteResponse.StatusCode != FtpStatusCode.FileActionOK && deleteResponse.StatusCode != FtpStatusCode.ActionNotTakenFileUnavailable)
                        {
                            return StatusCode(StatusCodes.Status500InternalServerError, $"Error deleting file {file}: {deleteResponse.StatusDescription}");
                        }
                    }
                }
            }
            var uploadUri = new Uri(ftpBaseUri, fileName);
            var uploadRequest = (FtpWebRequest)WebRequest.Create(uploadUri);
            uploadRequest.Method = WebRequestMethods.Ftp.UploadFile;
            uploadRequest.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);

            byte[] fileContents = await System.IO.File.ReadAllBytesAsync(localPath);
            uploadRequest.ContentLength = fileContents.Length;

            using (var requestStream = await uploadRequest.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(fileContents, 0, fileContents.Length);
            }

            using (var uploadResponse = (FtpWebResponse)await uploadRequest.GetResponseAsync())
            {
                if (uploadResponse.StatusCode == FtpStatusCode.ClosingData)
                {
                    return Ok("File replaced successfully.");
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Error uploading file: {uploadResponse.StatusDescription}");
                }
            }
        }

        private async Task<List<string>> ListFilesInDirectoryAsync(Uri directoryUri)
        {
            var fileList = new List<string>();

            var request = (FtpWebRequest)WebRequest.Create(directoryUri);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);

            try
            {
                using (var response = (FtpWebResponse)await request.GetResponseAsync())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        fileList.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error listing files in directory", ex);
            }

            return fileList;
        }
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteFile([FromForm] string remotePath)
        {
            if (string.IsNullOrEmpty(remotePath))
            {
                return BadRequest("Remote path is required.");
            }
            var ftpUri = new Uri(new Uri(_ftpServerAddress), remotePath.TrimStart('/'));
            var request = (FtpWebRequest)WebRequest.Create(ftpUri);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);

            using (var response = (FtpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode == FtpStatusCode.FileActionOK)
                {
                    return Ok("File deleted successfully.");
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Error deleting file: {response.StatusDescription}");
                }
            }
        }
    }
}
