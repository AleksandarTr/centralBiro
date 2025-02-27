using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace CentralBiro;

public class HttpHandler
{
    public static HttpHandler Instance { get; } = new HttpHandler();

    private string wwwPath = MainWindow.rootPathUrl + Path.DirectorySeparatorChar + "www";
    private string certPath = MainWindow.rootPathUrl + Path.DirectorySeparatorChar + "certs";
    private X509Certificate2 sslCertificate;
    private Dictionary<string, string> contentTypes = new Dictionary<string, string>
    {
        { ".html", "text/html; charset=utf-8" },
        { ".css", "text/css; charset=utf-8"},
        { ".js", "text/javascript; charset=utf-8"},
        { ".png", "image/png"}
    };
    
    private void getCertificate()
    {
        try
        {
            var cert = X509Certificate2.CreateFromPemFile(
                certPath + Path.DirectorySeparatorChar + "server.crt",
                certPath + Path.DirectorySeparatorChar + "server.key");
            sslCertificate = new X509Certificate2(cert.Export(X509ContentType.Pfx));
        }
        catch (Exception ex) {}
    }

    private string receiveRequest(SslStream stream)
    {
        byte[] buffer = new byte[2048];
        StringBuilder messageData = new StringBuilder();
        int bytes = -1;
        do
        {
            try { bytes = stream.Read(buffer, 0, buffer.Length); }
            catch (IOException _) { return ""; }
            
            Decoder decoder = Encoding.UTF8.GetDecoder();
            char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
            decoder.GetChars(buffer, 0, bytes, chars, 0);
            messageData.Append(chars);

            if (messageData.ToString().IndexOf("\r\n\r\n") > -1 || messageData.ToString().IndexOf("\n\n") > -1) break;
        } while (bytes > 0);
        
        return messageData.ToString();
    }

    private bool processRequest(string request, SslStream stream)
    {
        if (request == "") return false;
        
        int endInd = request.IndexOf(" HTTP/1.1");
        request = request.Remove(endInd, request.Length - endInd);
        int startInd = request.LastIndexOf("\n");
        if (startInd == -1) startInd = 0;
        request = request.Remove(0, startInd);
        string httpMethod = request.Split(' ')[0];
        string resourceUrl = request.Split(' ')[1];
        
        stream.Write(prepareHttpResponse(httpMethod, resourceUrl));
        return true;
    }

    private SslStream prepareSslStream(TcpClient client)
    {
        SslStream sslStream = new SslStream(client.GetStream(), false);

        try
        {
            sslStream.AuthenticateAsServer(sslCertificate, false, SslProtocols.Tls12 | SslProtocols.Tls13, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return sslStream;
    }
    
    private string httpHeader(int statusCode, string contentType, int contentLength)
    {
        string header = "HTTP/1.1 " + statusCode + "\n" +
                        "Server: simpleSSLServer\n" +
                        "Content-Type: " + contentType + 
                        "\nContent-Length" + contentLength + "\n\n";
        
        return header;
    }

    private byte[] getRequest(string resourceUrl, out int statusCode, out string contentType)
    {
        if (resourceUrl == "" || resourceUrl.EndsWith('/'))
            resourceUrl += "index.html";
        resourceUrl = resourceUrl.Replace('/', Path.DirectorySeparatorChar);
        
        string filePath = wwwPath + resourceUrl;
        if (!File.Exists(filePath))
        {
            statusCode = 404;
            contentType = "text";
            return "File not found"u8.ToArray();
        }

        byte[] fileData = File.ReadAllBytes(filePath); 
        bool knownType = contentTypes.TryGetValue(resourceUrl.Substring(resourceUrl.LastIndexOf('.')), out contentType);
        if (!knownType) contentType = "text";
        
        statusCode = 200;
        return fileData;
    }
    
    private byte[] prepareHttpResponse(string httpMethod, string resourceUrl)
    {
        int statusCode = 200;
        string contentType = "text";
        
        byte[] response = httpMethod switch
        {
            "GET" => getRequest(resourceUrl, out statusCode, out contentType),
            _ => "Could not process request"u8.ToArray()
        };
        
        byte[] header = Encoding.UTF8.GetBytes(httpHeader(statusCode, contentType, response.Length));
        return header.Concat(response).ToArray();
    }

    private HttpHandler()
    {
        getCertificate();
    }

    public void handleHttpConnection(TcpClient client)
    {
        SslStream stream = prepareSslStream(client);
        stream.ReadTimeout = 100;
        
        while (client.Connected)
        {
            string request = receiveRequest(stream);
            if (!processRequest(request, stream)) break;
            Thread.Sleep(100);
        }
        
        stream.Close();
        client.Close();
    }
}