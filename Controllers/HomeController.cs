using Microsoft.AspNetCore.Mvc;
using Restaurant.Repository.Interface;

namespace Restaurant.Controllers;

public class HomeController : Controller
{
    private readonly IRestaurantInfoRepository _infoRepository;

    public HomeController(IRestaurantInfoRepository infoRepository)
    {
        _infoRepository = infoRepository;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Location()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult Menu()
    {
        return View();
    }

    public IActionResult Reservation()
    {
        return View();
    }

    public IActionResult Admin()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}