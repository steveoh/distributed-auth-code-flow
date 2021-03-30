using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProtoBuf;
using StackExchange.Redis;

namespace auth_tickets.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class HomeController : ControllerBase
  {
    private readonly IServer redis;
    private readonly IDatabase database;

    public HomeController(IConnectionMultiplexer connectionMultiplexer)
    {
      var endpoint = connectionMultiplexer.GetEndPoints()[0];
      redis = connectionMultiplexer.GetServer(endpoint);
      database = connectionMultiplexer.GetDatabase();
    }
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
    [HttpGet("/my-role")]
    public IEnumerable<string> GetRoles()
    {
      return HttpContext.User.Claims
        .Where(claim => claim.Type == "agrcPLSS:UserRole")
        .DefaultIfEmpty(new Claim("agrcPLSS:UserRole", "anonymous"))
        .Select(x => x.Value);
    }

    [Authorize]
    [HttpGet("/my-logins")]
    public async Task<IEnumerable<string>> GetLogins()
    {
      var id = HttpContext.User.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier);
      var key = $"customer:{id.Value}";

      var bytes = await database.HashGetAsync(key, "data");

      using var memoryStream = new MemoryStream(bytes);

      var logins = Serializer.Deserialize<LoginSessions>(memoryStream);

      return logins.Locations.Select(key => key.ToString());
    }

    [Authorize("administrator")]
    [HttpGet("/flush")]
    public async Task<int> FlushSessions()
    {
      var keys = redis.Keys(pattern: "customer*").ToArray();

      await database.KeyDeleteAsync(keys);

      keys = redis.Keys(pattern: "authentication*").ToArray();

      await database.KeyDeleteAsync(keys);

      return keys.Length;
    }

    [Authorize("administrator")]
    [HttpGet("/get-all")]
    public IEnumerable<string> ListKeys()
    {
      var keys = redis.Keys(pattern: "*").ToArray();

      return keys.Select(x => x.ToString());
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
