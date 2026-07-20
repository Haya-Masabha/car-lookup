using System.Diagnostics;
using CarLookup.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarLookup.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var latestYear = DateTime.UtcNow.Year + 1;

        return View(new LookupViewModel
        {
            Years = Enumerable
                .Range(VehiclesController.EarliestModelYear, latestYear - VehiclesController.EarliestModelYear + 1)
                .Reverse()
                .ToList()
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
