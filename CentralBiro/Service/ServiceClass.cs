using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace CentralBiro.Service;

public abstract class ServiceClass
{
    protected delegate byte[] RequestDelegate(HttpHandler.Request request, out int statusCode, out string contentType);
    protected abstract Dictionary<Tuple<string, string>, RequestDelegate> RequestDelegates { get; }

    protected byte[] Serialize<T>(T obj)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T)); 
        MemoryStream writer = new MemoryStream();
        serializer.Serialize(writer, obj);
        return writer.ToArray();
    }
    
    public byte[] Execute(HttpHandler.Request request, out int statusCode, out string contentType)
    {
        string selectorUrl = request.ResourceUrl.Split("/").Length > 2 ? request.ResourceUrl.Split("/")[2] : "";
        Tuple<string, string> selector = new Tuple<string, string>(request.HttpMethod, selectorUrl);
        bool found = RequestDelegates.TryGetValue(selector, out RequestDelegate requestDelegate);
        if(found) return requestDelegate(request, out statusCode, out contentType);

        statusCode = 404;
        contentType = "text/plain";
        return "Could not find a service method to execute the request."u8.ToArray();
    }
}