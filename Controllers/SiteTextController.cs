using Microsoft.AspNetCore.Mvc;
using Restaurant.Repository.Interface;

namespace Restaurant.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SiteTextController : ControllerBase
{
    private readonly ISiteTextRepository _repository;

    public SiteTextController(ISiteTextRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public IActionResult Get([FromQuery] string lang = "ka")
    {
        var items = _repository.GetAll();

        var result = items.ToDictionary(
            t => t.KeyName,
            t => lang switch
            {
                "en" => t.ValueEn,
                "ru" => t.ValueRu,
                _ => t.ValueKa
            }
        );

        return Ok(result);
    }
}