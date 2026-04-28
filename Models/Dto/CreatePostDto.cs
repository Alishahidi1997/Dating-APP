using System.ComponentModel.DataAnnotations;
using API.Entities;

namespace API.Models.Dto;

public class CreatePostDto
{
    [Required]
    [MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    public PostVisibility Visibility { get; set; } = PostVisibility.Public;
    public List<int>? PhotoIds { get; set; }
}
