using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using CentralBiro.Service;
using Args = System.Collections.Generic.Dictionary<string, string>;

namespace CentralBiro;

public class InvalidHttpRequestException(string message) : Exception(message);

public class HttpHandler
{
    public static HttpHandler Instance { get; } = new HttpHandler();

    private readonly string _wwwPath = MainWindow.RootPathUrl + Path.DirectorySeparatorChar + "www";
    private readonly string _certPath = MainWindow.RootPathUrl + Path.DirectorySeparatorChar + "certs";
    private X509Certificate2 _sslCertificate;
    private readonly Dictionary<string, string> _contentTypes = new Dictionary<string, string>
    {
        { ".html", "text/html; charset=utf-8" },
        { ".css", "text/css; charset=utf-8"},
        { ".js", "text/javascript; charset=utf-8"},
        { ".png", "image/png"}
    };

    public struct Request(string httpMethod, string resourceUrl, Args args)
    {
        public string HttpMethod = httpMethod;
        public string ResourceUrl = resourceUrl;
        public Args Args = args;
    }

    private static Dictionary<string, ServiceClass> serviceClasses = new Dictionary<string, ServiceClass>()
    {
        { "login", LoginManager.Instance }
    };
    
    private void GetCertificate()
    {
        var cert = X509Certificate2.CreateFromPemFile(
            _certPath + Path.DirectorySeparatorChar + "server.crt",
            _certPath + Path.DirectorySeparatorChar + "server.key");
        _sslCertificate = new X509Certificate2(cert.Export(X509ContentType.Pfx));
    }

    private string? ReceiveRequest(SslStream stream)
    {
        byte[] buffer = new byte[2048];
        StringBuilder messageData = new StringBuilder();
        int bytes = -1;
        
        do
        {
            try { bytes = stream.Read(buffer, 0, buffer.Length); }
            catch (IOException _) { return null; }
            
            Decoder decoder = Encoding.UTF8.GetDecoder();
            char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
            decoder.GetChars(buffer, 0, bytes, chars, 0);
            messageData.Append(chars);

            if (messageData.ToString().IndexOf("\r\n\r\n") > -1 || messageData.ToString().IndexOf("\n\n") > -1) break;
        } while (bytes > 0);
        
        Console.WriteLine(messageData.ToString());
        return messageData.ToString();
    }

    private bool ProcessRequest(string? request, SslStream stream)
    {
        if (String.IsNullOrEmpty(request)) return false;
        Args args = GetRequestArguments(request);
        
        int endInd = request.IndexOf(" HTTP/1.1");
        request = request.Remove(endInd, request.Length - endInd);
        int startInd = request.LastIndexOf("\n");
        if (startInd == -1) startInd = 0;
        request = request.Remove(0, startInd);
        string httpMethod = request.Split(' ')[0];
        string resourceUrl = request.Split(' ')[1];
        
        GetUrlArguments(resourceUrl, args);
        stream.Write(PrepareHttpResponse(new Request(httpMethod, resourceUrl, args)));
        return true;
    }

    private SslStream? PrepareSslStream(TcpClient client)
    {
        SslStream sslStream = new SslStream(client.GetStream(), false);

        try
        {
            sslStream.AuthenticateAsServer(_sslCertificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
        }
        catch (Exception ex)
        {
            sslStream.Close();
            return null;
        }
        return sslStream;
    }
    
    private string HttpHeader(int statusCode, string contentType, int contentLength)
    {
        string header = "HTTP/1.1 " + statusCode + "\n" +
                        "Server: simpleSSLServer\n" +
                        "Content-Type: " + contentType + 
                        "\nContent-Length" + contentLength + "\n\n";
        
        return header;
    }

    private Args GetRequestArguments(string? request)
    {
        Args arguments = new Args();
        if (request == null) return arguments;
        int start = request.IndexOf("\r\n\r\n");
        if (start == -1) return arguments;
        start += 4;
        if (request.Substring(start) == "") return arguments;
        
        string[] keyValues = request.Substring(start).Split('&');
        foreach (string keyValue in keyValues)
        {
            string key = System.Uri.UnescapeDataString(keyValue.Split('=')[0]);
            string value = System.Uri.UnescapeDataString(keyValue.Split('=')[1]);
            arguments.Add(key, value);
        }
        
        return arguments;
    }

    private void GetUrlArguments(string url, Args args)
    {
        if(url.Split('?').Length < 2) return;
        string argString = url.Split('?')[1];
        string[] keyValues = argString.Split('&');
        foreach (string keyValue in keyValues)
        {
            string key = System.Uri.UnescapeDataString(keyValue.Split('=')[0]);
            string value = System.Uri.UnescapeDataString(keyValue.Split('=')[1]);
            args.Add(key, value);
        }
    }

    private byte[]? ServiceRequest(Request request, out int statusCode, out string contentType)
    {
        string service = request.ResourceUrl.Split('/')[1];
        bool serviceExists = serviceClasses.TryGetValue(service, out ServiceClass serviceClass);
        if (!serviceExists)
        {
            statusCode = 404;
            contentType = "";
            return null;
        }
        
        return serviceClass!.Execute(request, out statusCode, out contentType);
    }

    private byte[] GetRequest(Request request, out int statusCode, out string contentType)
    {
        if (request.ResourceUrl == "" || request.ResourceUrl.EndsWith('/'))
            request.ResourceUrl += "index.html";
        request.ResourceUrl = request.ResourceUrl.Replace('/', Path.DirectorySeparatorChar);
        
        string filePath = _wwwPath + request.ResourceUrl;
        if (!File.Exists(filePath))
        {
            statusCode = 404;
            contentType = "text";
            return "File not found"u8.ToArray();
        }

        byte[] fileData = File.ReadAllBytes(filePath); 
        bool knownType = _contentTypes.TryGetValue(request.ResourceUrl.Substring(request.ResourceUrl.LastIndexOf('.')), out contentType);
        if (!knownType) contentType = "text";
        
        statusCode = 200;
        return fileData;
    }
    
    private byte[] PrepareHttpResponse(Request request)
    {
        int statusCode = 200;
        string contentType = "text";
        
        byte[]? response = ServiceRequest(request, out statusCode, out contentType);
        if(response != null) return response;
        
        response = request.HttpMethod switch
        {
            "GET" => GetRequest(request, out statusCode, out contentType),
            _ => "Could not process request"u8.ToArray()
        };
        
        byte[] header = Encoding.UTF8.GetBytes(HttpHeader(statusCode, contentType, response.Length));
        return header.Concat(response).ToArray();
    }

    private HttpHandler()
    {
        GetCertificate();
    }

    public void HandleHttpConnection(TcpClient client)
    {
        SslStream? stream = PrepareSslStream(client);
        if (stream == null)
        {
            client.Close();
            return;
        }
        stream.ReadTimeout = 100;
        
        while (client.Connected)
        {
            string request = ReceiveRequest(stream);
            if (!ProcessRequest(request, stream)) break;
            Thread.Sleep(100);
        }
        
        stream.Close();
        client.Close();
    }
}