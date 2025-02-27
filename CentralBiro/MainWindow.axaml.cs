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
    public static string rootPathUrl { get; } = Directory.GetCurrentDirectory();
    private static bool isServerActive = false;
    private Thread requestReciever;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    public void log(string text)
    {
        Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            ServerLog.Children.Add(new TextBlock { Text = text });
        });
    }

    private void StartServer(object? sender, RoutedEventArgs e)
    {
        StartServerButton.IsEnabled = false;
        StopServerButton.IsEnabled = true;
        isServerActive = true;

        requestReciever = new Thread(() =>
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 5000);
            tcpListener.Start();
            log("Server started");

            while (isServerActive)
            {
                while (tcpListener.Pending())
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    log("Client connected: " + client.Client.RemoteEndPoint);
                    
                    new Thread(() =>
                    {
                        string remoteEndPoint = client.Client.RemoteEndPoint.ToString();
                        HttpHandler.Instance.handleHttpConnection(client);
                        log("Client disconnected: " + remoteEndPoint);
                    }).Start();
                }
                
                Thread.Sleep(100);
            }

            tcpListener.Stop();
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() => { 
                StartServerButton.IsEnabled = true;
                StopServerButton.IsEnabled = false;
            });
        });
        requestReciever.Start();
    }

    private void StopServer(object? sender, RoutedEventArgs e)
    {
        log("Server stopped");
        isServerActive = false;
    }
}