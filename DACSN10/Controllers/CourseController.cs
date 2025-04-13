
using Microsoft.AspNetCore.Mvc;

namespace DACSN10.Controllers
{
    public class CourseController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
