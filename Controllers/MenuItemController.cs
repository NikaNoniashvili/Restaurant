using Microsoft.AspNetCore.Mvc;
using Restaurant.Repository.Interface;

namespace Restaurant.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MenuItemController : ControllerBase
{
    private readonly IMenuItemRepository _repository;

    public MenuItemController(IMenuItemRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public IActionResult Get([FromQuery] string lang = "ka")
    {
        var items = _repository.GetActive(); // ✅ მხოლოდ აქტიური კერძები საიტისთვის

        var result = items.Select(m => new
        {
            m.Id,
            m.CategoryId,
            Name = lang switch
            {
                "en" => m.NameEn,
                "ru" => m.NameRu,
                _ => m.NameKa
            },
            Description = lang switch
            {
                "en" => m.DescriptionEn,
                "ru" => m.DescriptionRu,
                _ => m.DescriptionKa
            },
            m.Price,
            m.ImageUrl,
            m.SortOrder
        }).ToList();

        return Ok(result);
    }
}