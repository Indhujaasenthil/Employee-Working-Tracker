
using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/files")]
[Authorize]
public class ProjectFilesController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".txt"] = "text/plain",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg"
        };

    private readonly AppDbContext _database;
    private readonly IWebHostEnvironment _environment;

    public ProjectFilesController(
        AppDbContext database,
        IWebHostEnvironment environment)
    {
        _database = database;
        _environment = environment;
    }

    private string StorageDirectory =>
        Path.Combine(
            _environment.ContentRootPath,
            "PrivateUploads",
            "Projects");

    // GET: api/projects/1/files
    [HttpGet]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> GetFiles(int projectId)
    {
        var exists = await _database.Projects
            .AnyAsync(project => project.Id == projectId);

        if (!exists)
        {
            return NotFound(new
            {
                message = "Project was not found."
            });
        }

        var files = await _database.ProjectFiles
            .AsNoTracking()
            .Where(file => file.ProjectId == projectId)
            .OrderByDescending(file => file.UploadedAt)
            .Select(file => new
            {
                file.Id,
                file.ProjectId,
                file.OriginalFileName,
                file.ContentType,
                file.FileSize,
                file.UploadedAt
            })
            .ToListAsync();

        return Ok(files);
    }

    // POST: api/projects/1/files
    [HttpPost]
    [Authorize(Roles = "Administrator,Manager")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<IActionResult> UploadFile(
        int projectId,
        [FromForm] IFormFile? file)
    {
        var projectExists = await _database.Projects
            .AnyAsync(project => project.Id == projectId);

        if (!projectExists)
        {
            return NotFound(new
            {
                message = "Project was not found."
            });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please select a file."
            });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new
            {
                message = "Maximum file size is 10 MB."
            });
        }

        var originalName = Path.GetFileName(
            file.FileName.Replace('\\', '/'));

        if (string.IsNullOrWhiteSpace(originalName) ||
            originalName.Length > 255)
        {
            return BadRequest(new
            {
                message = "Invalid file name."
            });
        }

        var extension = Path.GetExtension(originalName);

        if (!AllowedTypes.TryGetValue(
            extension, out var contentType))
        {
            return BadRequest(new
            {
                message =
                    "Only PDF, TXT, PNG and JPG files are allowed."
            });
        }

        Directory.CreateDirectory(StorageDirectory);

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(StorageDirectory, storedName);

        try
        {
            await using (var stream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            var projectFile = new ProjectFile
            {
                ProjectId = projectId,
                OriginalFileName = originalName,
                StoredFileName = storedName,
                ContentType = contentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            _database.ProjectFiles.Add(projectFile);
            await _database.SaveChangesAsync();

            return Ok(new
            {
                message = "Project file uploaded successfully.",
                projectFile.Id,
                projectFile.ProjectId,
                projectFile.OriginalFileName,
                projectFile.ContentType,
                projectFile.FileSize,
                projectFile.UploadedAt
            });
        }
        catch
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            throw;
        }
    }

    // GET: api/projects/1/files/2/download
    [HttpGet("{fileId:int}/download")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> DownloadFile(
        int projectId,
        int fileId)
    {
        var file = await _database.ProjectFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == fileId &&
                item.ProjectId == projectId);

        if (file == null)
        {
            return NotFound(new
            {
                message = "File was not found."
            });
        }

        var path = GetStoredFilePath(file.StoredFileName);

        if (!System.IO.File.Exists(path))
        {
            return NotFound(new
            {
                message = "Stored file was not found."
            });
        }

        var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "no-store";

        return File(
            stream,
            file.ContentType,
            file.OriginalFileName);
    }

    // GET: api/projects/1/files/2/preview
    [HttpGet("{fileId:int}/preview")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> PreviewFile(
        int projectId,
        int fileId)
    {
        var file = await _database.ProjectFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == fileId &&
                item.ProjectId == projectId);

        if (file == null)
        {
            return NotFound(new
            {
                message = "File was not found."
            });
        }

        var allowedPreviewTypes = new[]
        {
            "application/pdf",
            "text/plain",
            "image/png",
            "image/jpeg"
        };

        if (!allowedPreviewTypes.Contains(
            file.ContentType,
            StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "This file type cannot be previewed."
            });
        }

        var path = GetStoredFilePath(file.StoredFileName);

        if (!System.IO.File.Exists(path))
        {
            return NotFound(new
            {
                message = "Stored file was not found."
            });
        }

        var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Content-Security-Policy"] =
            "default-src 'none'; sandbox";

        return File(stream, file.ContentType);
    }

    // DELETE: api/projects/1/files/2
    [HttpDelete("{fileId:int}")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> DeleteFile(
        int projectId,
        int fileId)
    {
        var file = await _database.ProjectFiles
            .FirstOrDefaultAsync(item =>
                item.Id == fileId &&
                item.ProjectId == projectId);

        if (file == null)
        {
            return NotFound(new
            {
                message = "File was not found."
            });
        }

        var path = GetStoredFilePath(file.StoredFileName);

        _database.ProjectFiles.Remove(file);
        await _database.SaveChangesAsync();

        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        return NoContent();
    }

    private string GetStoredFilePath(string storedFileName)
    {
        return Path.Combine(
            StorageDirectory,
            Path.GetFileName(storedFileName));
    }
}
