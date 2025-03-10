using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.IO;
using System.Net;
using Avalonia.Media;
using CentralBiro.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CentralBiro;

public partial class MainWindow : Window
{
    private static string RootPathUrl { get; } = Directory.GetCurrentDirectory();
    private static readonly string CertPath = RootPathUrl + Path.DirectorySeparatorChar + "certs";
    private static readonly string RootPath = RootPathUrl + Path.DirectorySeparatorChar + "www";

    private WebApplication? _app;
    private const int HttpPort = 4999;
    private const int HttpsPort = 5000;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    public bool Log(string text, Color? color = null)
    {
        try
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                ServerLog.Children.Add(new TextBlock
                {
                    Text = text,
                    Foreground = new SolidColorBrush(color ?? Colors.White)
                });
            });
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed logging:" + text);
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    class WindowLogger(MainWindow window) : ILogger
    {
        private readonly MainWindow _window = window;
        
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None) return false;
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var logMessage = formatter(state, exception);
            Color color = logLevel switch
            {
                LogLevel.Trace => Colors.DimGray,
                LogLevel.Debug => Colors.Gray,
                LogLevel.Information => Colors.White,
                LogLevel.Warning => Colors.Yellow,
                LogLevel.Error => Colors.Orange,
                LogLevel.Critical => Colors.Red,
                _ => Colors.White
            };
            _window.Log(logMessage, color);
        }
    }

    public class WindowLoggerProvider(MainWindow window) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new WindowLogger(window);
        }

        public void Dispose()
        { }
    }

    private void StartServer(object? sender, RoutedEventArgs e)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Any, HttpPort);
            options.Listen(IPAddress.Any, HttpsPort, listenOptions =>
            {
                listenOptions.UseHttps(CertPath + Path.DirectorySeparatorChar + "server.pfx", "Test1234");
            });
        });
        builder.Services.AddControllers();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new WindowLoggerProvider(this));
        
        _app = builder.Build();
        _app.UseRouting();
        _app.MapControllers();
        _app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(RootPath)
        });
        _app.Start();
        StartServerButton.IsEnabled = false;
        StopServerButton.IsEnabled = true;
    }

    private void StopServer(object? sender, RoutedEventArgs e)
    {
        StopServerButton.IsEnabled = false;
        _app?.DisposeAsync();
        StartServerButton.IsEnabled = true;
        Log("Server stopped.");
    }
}