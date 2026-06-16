using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

namespace whstore.Controllers
{
    public class AccountController : Controller
    {
        // ভিউ ফাইলের পাথটি স্পষ্টভাবে উল্লেখ করে দেওয়া হলো যাতে এরর না দেয়
        [HttpGet]
        [Route("Account/Login")]
        public IActionResult Login()
        {
            return View("~/Views/Account/Login.cshtml");
        }

        // গুগল লগইন ফিচার
        [Route("Account/GoogleLogin")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // গুগল লগইন রেসপন্স
        [Route("Account/GoogleResponse")]
        public IActionResult GoogleResponse()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}