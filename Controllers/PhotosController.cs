using System.Security.Claims;
using API.Models.Dto;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PhotosController(IPhotoService photoService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file, CancellationToken ct)
    {
        var photo = await photoService.AddPhotoAsync(UserId, file, ct);
        if (photo == null)
            return BadRequest("Invalid file. Allowed: jpg, jpeg, png, gif, webp. Max 5MB.");

        return CreatedAtAction(nameof(AddPhoto), new PhotoDto
        {
            Id = photo.Id,
            Url = photo.Url,
            IsMain = photo.IsMain
        });
    }

    [HttpDelete("{photoId:int}")]
    public async Task<ActionResult> DeletePhoto(int photoId, CancellationToken ct)
    {
        if (!await photoService.DeletePhotoAsync(UserId, photoId, ct))
            return BadRequest("Cannot delete main photo or photo not found");

        return NoContent();
    }

    [HttpPut("{photoId:int}/set-main")]
    public async Task<ActionResult> SetMain(int photoId, CancellationToken ct)
    {
        if (!await photoService.SetMainPhotoAsync(UserId, photoId, ct))
            return BadRequest("Photo not found");

        return NoContent();
    }
}
