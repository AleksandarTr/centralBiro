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

    private void ListenServer(TcpListener listener, bool secure)
    {
        while (listener.Pending())
        {
            TcpClient client = listener.AcceptTcpClient();
            Log("Client connected: " + client.Client.RemoteEndPoint);
                
            new Thread(() =>
            {
                string remoteEndPoint = client.Client.RemoteEndPoint.ToString();
                HttpHandler.Instance.HandleHttpConnection(client, secure);
                Log("Client disconnected: " + remoteEndPoint);
            }).Start();
        }
            
        Thread.Sleep(100);
    }

    private void StartServer(object? sender, RoutedEventArgs e)
    {
        StartServerButton.IsEnabled = false;
        StopServerButton.IsEnabled = true;
        _isServerActive = true;

        _requestReciever = new Thread(() =>
        {
            TcpListener httpsListener = new TcpListener(IPAddress.Any, 5000);
            TcpListener httpListener = new TcpListener(IPAddress.Any, 4999);
            httpsListener.Start();
            httpListener.Start();
            Log("Server started");

            Thread httpThread = new Thread(() =>
            {
                while(_isServerActive) ListenServer(httpListener, false);
                httpListener.Stop();
            });
            httpThread.Start();
            while (_isServerActive) ListenServer(httpsListener, true);

            httpsListener.Stop();
            httpThread.Join();
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