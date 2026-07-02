using Microsoft.AspNetCore.Mvc;

namespace whstore.Controllers
{
    public class GameController : Controller
    {
        // Action method to display a list of games
        public IActionResult Index()
        {
            // In a real application, you would fetch game data from a database or service.
            // For now, we'll just return a simple view.
            return View();
        }

        // Action method to play a specific game
        public IActionResult Play(string gameUrl)
        {
            ViewBag.GameUrl = gameUrl;
            return View();
        }
    }
}
