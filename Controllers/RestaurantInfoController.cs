using Microsoft.AspNetCore.Mvc;
using Restaurant.Repository.Interface;

namespace Restaurant.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RestaurantInfoController : ControllerBase
{
    private readonly IRestaurantInfoRepository _repository;

    public RestaurantInfoController(IRestaurantInfoRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public IActionResult Get([FromQuery] string lang = "ka")
    {
        var info = _repository.Get();
        if (info == null)
            return NotFound();

        var result = new
        {
            info.Id,
            Address = lang switch
            {
                "en" => info.AddressEn,
                "ru" => info.AddressRu,
                _ => info.AddressKa
            },
            info.Phone,
            info.Email,
            WorkingHours = lang switch
            {
                "en" => info.WorkingHoursEn,
                "ru" => info.WorkingHoursRu,
                _ => info.WorkingHoursKa
            },
            info.FacebookUrl,
            info.InstagramUrl,
            info.MapEmbedUrl
        };

        return Ok(result);
    }
}