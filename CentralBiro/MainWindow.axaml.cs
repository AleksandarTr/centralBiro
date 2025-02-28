using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace CentralBiro;

public partial class MainWindow : Window
{
    public static string RootPathUrl { get; } = Directory.GetCurrentDirectory();
    private bool _isServerActive = false;
    private bool _isClosing = false;
    private Thread _requestReciever;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        _isClosing = true;
        if(_isServerActive) _isServerActive = false;
    }

    public void Log(string text)
    {
        if(_isClosing) return;
        Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            ServerLog.Children.Add(new TextBlock { Text = text });
        });
    }

    private void StartServer(object? sender, RoutedEventArgs e)
    {
        StartServerButton.IsEnabled = false;
        StopServerButton.IsEnabled = true;
        _isServerActive = true;

        _requestReciever = new Thread(() =>
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 5000);
            tcpListener.Start();
            Log("Server started");

            while (_isServerActive)
            {
                while (tcpListener.Pending())
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Log("Client connected: " + client.Client.RemoteEndPoint);
                    
                    new Thread(() =>
                    {
                        string remoteEndPoint = client.Client.RemoteEndPoint.ToString();
                        HttpHandler.Instance.HandleHttpConnection(client);
                        Log("Client disconnected: " + remoteEndPoint);
                    }).Start();
                }
                
                Thread.Sleep(100);
            }

            tcpListener.Stop();
            Log("Server stopped");
            if (_isClosing) return;
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() => { 
                StartServerButton.IsEnabled = true;
                StopServerButton.IsEnabled = false;
            });
        });
        _requestReciever.Start();
    }

    private void StopServer(object? sender, RoutedEventArgs e)
    {
        _isServerActive = false;
    }
}