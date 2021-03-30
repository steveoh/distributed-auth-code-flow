using System.Collections.Generic;
using ProtoBuf;
using UAParser;

namespace auth_tickets {
  [ProtoContract]
  public class LoginSessions {
    public LoginSessions() {}

    [ProtoMember(1)]
    public List<Session> Locations { get; set; } = new List<Session>();
  }

  [ProtoContract]
  public class Session {
    public Session() {}
    public Session(ClientInfo client, string state, string city, string authKey)
    {
      Browser = client.UA.Family;
      OperatingSystem = client.OS.Family;
      Device = client.Device.Family;
      State = state;
      City = city;
      AuthKey = authKey;
    }
    [ProtoMember(1)]
    public string Browser { get; }
    [ProtoMember(2)]
    public string OperatingSystem { get; }
    [ProtoMember(3)]
    public string Device { get; }
    [ProtoMember(4)]
    public string State { get; }
    [ProtoMember(5)]
    public string City { get; }
    [ProtoMember(6)]
    public string AuthKey { get; }
    public override string ToString() => $"{Browser} on {OperatingSystem} using a {Device} at {City}, {State}";
  }
}
