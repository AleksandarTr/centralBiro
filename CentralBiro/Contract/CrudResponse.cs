using System;
using System.Collections.Generic;
using CentralBiro.Database;
using CentralBiro.Service;

namespace CentralBiro.Contract;

public struct CrudResponse(Object result, int count, Dictionary<int, string> userMap)
{
    public Object Result { get; set; } = result;
    public int Count { get; set; } = count;
    public Dictionary<int, string> UserMap { get; set; } = userMap;
    
    public CrudResponse(): this(null, -1, []) {}
}