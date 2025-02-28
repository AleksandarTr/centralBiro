using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CentralBiro;

public class LoginManager : ServiceClass
{
    protected override Dictionary<Tuple<string, string>, RequestDelegate> RequestDelegates { get; }

    private LoginManager()
    {
        RequestDelegates = new Dictionary<Tuple<string, string>, RequestDelegate>
        {
            { new Tuple<string, string>("POST", ""), LoginRequest}
        };
    }

    public static LoginManager Instance { get; } = new LoginManager();

    [Serializable, XmlRoot("LoginResponse")]
    public struct LoginResponse(bool success, string token)
    {
        public bool Success = success;
        public string Token = token;
    }

    public byte[] LoginRequest(HttpHandler.Request request, out int statusCode, out string contentType)
    {
        statusCode = 200;
        contentType = "text/plain";
        return Serialize(new LoginResponse(false, ""));
    }
}