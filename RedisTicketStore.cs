using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using ProtoBuf;
using UAParser;

namespace auth_tickets
{
  public class RedisTicketStore : ITicketStore
  {
    private const string KeyPrefix = "authentication-ticket:";
    private readonly IDistributedCache cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Parser _parser;
    private readonly WebServiceClient _ipClient;
    public RedisTicketStore(IDistributedCache cache, IHttpContextAccessor httpContextAccessor, WebServiceClient maxMindClient)
    {
      this.cache = cache;
      _httpContextAccessor = httpContextAccessor;
      _parser = Parser.GetDefault();
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
      var guid = Guid.NewGuid();
      var key = KeyPrefix + guid.ToString();

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

      byte[] val = SerializeToBytes(ticket);
      cache.Set(key, val, options);

      var id = ticket.Principal.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier);
      var sessionKey = $"customer:{id.Value}";

      RenewSessions(sessionKey, key, ticket.Properties.ExpiresUtc);

      return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
      RemoveSession(key);

      cache.Remove(key);

      return Task.FromResult(0);
    }

    public Task<AuthenticationTicket> RetrieveAsync(string key)
    {
      AuthenticationTicket ticket;
      byte[] bytes = cache.Get(key);
      ticket = DeserializeFromBytes(bytes);

      return Task.FromResult(ticket);
    }

    private static byte[] SerializeToBytes(AuthenticationTicket source)
    {
      return TicketSerializer.Default.Serialize(source);
    }

    private static AuthenticationTicket DeserializeFromBytes(byte[] source)
    {
      return source == null ? null : TicketSerializer.Default.Deserialize(source);
    }

    private void RenewSessions(string sessionKey, string authKey, DateTimeOffset? expiresUtc)
    {
      var options = new DistributedCacheEntryOptions();

      if (expiresUtc.HasValue)
      {
        options.SetAbsoluteExpiration(expiresUtc.Value);
      }

      if (_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("user-agent", out var ua))
      {

        var client = _parser.Parse(ua);
        var session = new Session(client, state, city, authKey);

        var logins = RetrieveLoginSessions(sessionKey);
        logins.Locations.Add(session);

        WriteSessionCache(sessionKey, logins, options);
      }
    }

    private void RemoveSession(string authKey)
    {
      AuthenticationTicket ticket;
      byte[] bytes = cache.Get(authKey);
      ticket = DeserializeFromBytes(bytes);

      var sessionKey = GetSessionKey(ticket);
      var logins = RetrieveLoginSessions(sessionKey);

      var endingSession = logins.Locations.Single(session => session.AuthKey == authKey);
      logins.Locations.Remove(endingSession);

      if (logins.Locations.Count == 0) {
        cache.Remove(sessionKey);

        return;
      }

      WriteSessionCache(sessionKey, logins);
    }

    private LoginSessions RetrieveLoginSessions(string key)
    {
      var bytes = cache.Get(key);

      if (bytes is null)
      {
        return new LoginSessions();
      }

      using var memoryStream = new MemoryStream(bytes);

      return Serializer.Deserialize<LoginSessions>(memoryStream);
    }

    private static string GetSessionKey(AuthenticationTicket ticket)
    {
      var id = ticket.Principal.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);

      return $"customer:{id.Value}";
    }

    private void WriteSessionCache(string sessionKey, LoginSessions logins, DistributedCacheEntryOptions options = null)
    {
      if (options is null)
      {
        options = new DistributedCacheEntryOptions();
      }

      using var memoryStream = new MemoryStream();
      Serializer.Serialize(memoryStream, logins);

      cache.Set(sessionKey, memoryStream.ToArray(), options);
    }
  }
}
