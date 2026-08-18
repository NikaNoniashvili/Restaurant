using Microsoft.AspNetCore.Mvc;
using Restaurant.Repository.Interface;

namespace Restaurant.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MenuCategoryController : ControllerBase
{
    private readonly IMenuCategoryRepository _repository;

    public MenuCategoryController(IMenuCategoryRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public IActionResult Get([FromQuery] string lang = "ka")
    {
        var items = _repository.GetActive(); // ✅ მხოლოდ აქტიური კატეგორიები საიტისთვის

        var result = items.Select(c => new
        {
            c.Id,
            Name = lang switch
            {
                "en" => c.NameEn,
                "ru" => c.NameRu,
                _ => c.NameKa
            },
            c.SortOrder
        }).ToList();

        return Ok(result);
    }
}