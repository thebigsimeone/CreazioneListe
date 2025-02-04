using CreazioneListe.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreazioneListe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SftpController : ControllerBase
    {
        private readonly SftpService _sftpService;

        public SftpController(SftpService sftpService)
        {
            _sftpService = sftpService;
        }

        [HttpGet("list-directories")]
        public IActionResult ListDirectories([FromQuery] string remotePath)
        {
            try
            {
                var directories = _sftpService.ListDirectories(remotePath);
                return Ok(directories);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("list-files")]
        public IActionResult ListFiles([FromQuery] string remotePath)
        {
            try
            {
                var files = _sftpService.ListFiles(remotePath);
                return Ok(files);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("download-file")]
        public IActionResult DownloadFile([FromQuery] string remoteFilePath)
        {
            try
            {
                var stream = _sftpService.DownloadFile(remoteFilePath);
                var fileName = Path.GetFileName(remoteFilePath);
                return File(stream, "application/octet-stream", fileName);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
