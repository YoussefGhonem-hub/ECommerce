using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public FilesController(IWebHostEnvironment env)
    {
        _env = env;
    }

    //[HttpPost("upload-product")]
    //public async Task<IActionResult> UploadProduct([FromForm] IFormFile file)
    //{
    //    if (file == null || file.Length == 0)
    //        return BadRequest("File is empty");

    //    var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "products");
    //    Directory.CreateDirectory(uploadsRoot);

    //    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
    //    var filePath = Path.Combine(uploadsRoot, fileName);

    //    await using (var stream = System.IO.File.Create(filePath))
    //    {
    //        await file.CopyToAsync(stream);
    //    }

    //    var url = $"/uploads/products/{fileName}";
    //    return Ok(new { url });
    //}
}
