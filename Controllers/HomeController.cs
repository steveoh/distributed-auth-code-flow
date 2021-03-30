using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace auth_tickets.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class HomeController : ControllerBase
  {
    [HttpGet("/")]
    public string Home()
    {
      return "Hello World";
    }

    [HttpGet("/logout")]
    public async Task Logout()
    {
      await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpGet("/secure")]
    public IEnumerable<string> Secure()
    {
      return HttpContext.User.Claims.Select(x => $"{x.Type}:{x.Value}");
    }

    [HttpGet("/authentication/access-denied")]
    public string Denied()
    {
      return "access-denied";
    }

    [HttpGet("/authentication/login")]
    public string Login()
    {
      return "login";
    }
  }
}
