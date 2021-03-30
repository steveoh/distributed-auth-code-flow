using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using UAParser;

namespace auth_tickets
{
  public class RedisTicketStore : ITicketStore
  {
    private const string KeyPrefix = "authentication-ticket-";
    private readonly IDistributedCache cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
private readonly Parser _parser;
    public RedisTicketStore(IDistributedCache cache, IHttpContextAccessor httpContextAccessor)
    {
      this.cache = cache;
      _httpContextAccessor = httpContextAccessor;
      _parser = Parser.GetDefault();
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
      var guid = Guid.NewGuid();
      var key = KeyPrefix + guid.ToString();

      if (_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("user-agent", out var ua))
      {
        var client = _parser.Parse(ua);
        var device = $"{client.UA.Family} on {client.OS.Family} using a {client.Device.Family}";
      }

      await RenewAsync(key, ticket);

      return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
      var options = new DistributedCacheEntryOptions();
      var expiresUtc = ticket.Properties.ExpiresUtc;

      if (expiresUtc.HasValue)
      {
        options.SetAbsoluteExpiration(expiresUtc.Value);
      }

      if (!ticket.Properties.Items.ContainsKey("key"))
      {
        ticket.Properties.Items.Add("key", key);
      }

      byte[] val = SerializeToBytes(ticket);
      cache.Set(key, val, options);

      return Task.FromResult(0);
    }

    public Task<AuthenticationTicket> RetrieveAsync(string key)
    {
      AuthenticationTicket ticket;
      byte[] bytes = cache.Get(key);
      ticket = DeserializeFromBytes(bytes);

      return Task.FromResult(ticket);
    }

    public Task RemoveAsync(string key)
    {
      cache.Remove(key);

      return Task.FromResult(0);
    }

    private static byte[] SerializeToBytes(AuthenticationTicket source)
    {
      return TicketSerializer.Default.Serialize(source);
    }

    private static AuthenticationTicket DeserializeFromBytes(byte[] source)
    {
      return source == null ? null : TicketSerializer.Default.Deserialize(source);
    }
  }
}
