using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

namespace CentralBiro;

public partial class MainWindow : Window
{
    private static string rootPathUrl = Directory.GetCurrentDirectory();
    private static string certPath;
    private static string wwwPath;
    private static X509Certificate2 sslCertificate;
    private static bool isReadingDetailsComplete = false;
    private Thread thread;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    private void getCertificate()
    {
        try
        {
            var cert = X509Certificate2.CreateFromPemFile(
                certPath + Path.DirectorySeparatorChar + "server.crt",
                certPath + Path.DirectorySeparatorChar + "server.key");
            sslCertificate = new X509Certificate2(cert.Export(X509ContentType.Pfx, "Agromehanizacija2003"), "Agromehanizacija2003");
        }
        catch (Exception ex) {}
    }

    private string httpHeader(int statusCode, string contentType, int contentLength)
    {
        string header = "HTTP/1.1 " + statusCode.ToString() + "\n" +
                        "Server: simpleSSLServer\n" +
                        "Content-Type: " + contentType + 
                        "\nContent-Length" + contentLength.ToString() + "\n\n";
        
        return header;
    }

    private string prepareHttpResponse(string httpMethod, string resourceUrl, SslStream sslStream)
    {
        if (httpMethod != "GET") return "";
        if (resourceUrl == "" || resourceUrl.EndsWith("/"))
            resourceUrl += "index.html";
        resourceUrl = resourceUrl.Replace('/', Path.DirectorySeparatorChar);
        string filePath = wwwPath + Path.DirectorySeparatorChar + resourceUrl;
        if (!File.Exists(filePath)) return httpHeader(404, "Not Found", 0);

        string fileData = File.ReadAllText(filePath);
        string contentType = resourceUrl.Substring(resourceUrl.LastIndexOf('.')) switch
        {
            "html" => "text/html; charset=utf-8",
            "css" => "text/css; charset=utf-8",
            "js" => "text/javascript; charset=utf-8",
            _ => ""
        }; 
        
        return httpHeader(200, contentType, fileData.Length) + "\r\n\r\n" + fileData;
    }

    private void processRequest(SslStream stream)
    {
        byte[] buffer = new byte[2048];
        StringBuilder messageData = new StringBuilder();
        int bytes = -1;
        do
        {
            bytes = stream.Read(buffer, 0, buffer.Length);
            Decoder decoder = Encoding.UTF8.GetDecoder();
            char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
            decoder.GetChars(buffer, 0, bytes, chars, 0);
            messageData.Append(chars);

            if (messageData.ToString().IndexOf("\r\n\r\n") > -1 || messageData.ToString().IndexOf("\n\n") > -1) break;
        } while (bytes > 0);
        
        int endInd = messageData.ToString().IndexOf(" HTTP/1.1");
        messageData.Remove(endInd, messageData.ToString().Length - endInd);
        int startInd = messageData.ToString().LastIndexOf("\n");
        if (startInd == -1) startInd = 0;
        messageData.Remove(0, startInd);
        string httpMethod = messageData.ToString().Split(' ')[0];
        string resourceUrl = messageData.ToString().Split(' ')[1];
        
        stream.Write(Encoding.UTF8.GetBytes(prepareHttpResponse(httpMethod, resourceUrl, stream))); 
    }

    private SslStream prepareSslStream(TcpClient client)
    {
        isReadingDetailsComplete = false;
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

    private void StartServer(object? sender, RoutedEventArgs e)
    {
        certPath = rootPathUrl + Path.DirectorySeparatorChar + "certs";
        wwwPath = rootPathUrl + Path.DirectorySeparatorChar + "www";
        getCertificate();

        thread = new Thread(() =>
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 5000);
            tcpListener.Start();

            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                SslStream stream = prepareSslStream(client);
                processRequest(stream);
                stream.Close();
                client.Close();
            }
        });
        thread.Start();
    }
}