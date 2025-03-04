using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.IO;
using System.Net;
using System.Net.Sockets;
using CentralBiro.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CentralBiro;

public partial class MainWindow : Window
{
    public static string RootPathUrl { get; } = Directory.GetCurrentDirectory();
    private static readonly string CertPath = MainWindow.RootPathUrl + Path.DirectorySeparatorChar + "certs";
    private static readonly string RootPath = MainWindow.RootPathUrl + Path.DirectorySeparatorChar + "www";

    private WebApplication _app;
    private const int HttpPort = 4999;
    private const int HttpsPort = 5000;
    
    public MainWindow()
    {
        InitializeComponent();
        
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Any, HttpPort);
            options.Listen(IPAddress.Any, HttpsPort, listenOptions =>
            {
                listenOptions.UseHttps(CertPath + Path.DirectorySeparatorChar + "server.pfx", "Test1234");
            });
        });
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keeps property names as-is
            options.JsonSerializerOptions.WriteIndented = true; // Pretty-print JSON
        });
        builder.Logging.AddConsole();
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "My API",
                Version = "v1",
                Description = "An example API with Swagger"
            });
        });
        
        
        _app = builder.Build();
        _app.UseRouting();
        _app.MapControllers();
        _app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(RootPath)
        });
        _app.UseSwagger();
        _app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1"); // API Docs URL
            c.RoutePrefix = "swagger";
        });
    }

    private void StartServer(object? sender, RoutedEventArgs e)
    {
        _app.Start();
        StartServerButton.IsEnabled = false;
        StopServerButton.IsEnabled = true;
    }

    private void StopServer(object? sender, RoutedEventArgs e)
    {
        _app.StopAsync().Wait();
        StartServerButton.IsEnabled = true;
        StopServerButton.IsEnabled = false;
    }
}