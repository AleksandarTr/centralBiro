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

    private SslStream prepareSslStream(TcpClient client)
    {
        isReadingDetailsComplete = false;
        SslStream sslStream = new SslStream(client.GetStream(), false);

        try
        {
            sslStream.AuthenticateAsServer(sslCertificate, clientCertificateRequired: false,
                enabledSslProtocols: SslProtocols.None, checkCertificateRevocation: false);
        }
        catch (Exception ex) {}
        return sslStream;
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        certPath = rootPathUrl + Path.DirectorySeparatorChar + "certs";
        wwwPath = rootPathUrl + Path.DirectorySeparatorChar + "www";
        getCertificate();
        Console.Out.WriteLine(sslCertificate.GetSerialNumber());

        thread = new Thread(() =>
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 443);
            tcpListener.Start();

            while (true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                SslStream stream = prepareSslStream(client);
                stream.Close();
                client.Close();
            }
        });
    }
}