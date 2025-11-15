using Microsoft.AspNetCore.Mvc;

namespace okem_social.Controllers;

// [Authorize]
public class SettingsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

