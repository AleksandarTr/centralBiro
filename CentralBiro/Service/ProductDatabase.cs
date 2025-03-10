using System;
using System.Collections.Generic;
using System.Linq;
using CentralBiro.Database;
using Microsoft.EntityFrameworkCore;

namespace CentralBiro.Service;

public class ProductDatabase
{
    public static ProductDatabase Instance { get; } = new();
    private ProductDatabase() {}
    private readonly Dictionary<int, ProductType> _productTypes = new();
    private readonly Dictionary<string, int> _productTypesByName = new();

    private void CacheProductType(ProductType productType)
    {
        lock (_productTypesByName)
        lock (_productTypes)
        {
            if (_productTypes.ContainsKey(productType.Id)) return;
            int nextSerialNumber = 0;
            try{nextSerialNumber = new CentralContext().Products.Max(p => p.SerialNumber) + 1;}
            catch(InvalidOperationException){nextSerialNumber = 1;}
            
            ProductType cache = new ProductType(productType.Name, productType.Id, nextSerialNumber);
            _productTypesByName.Add(productType.Name, productType.Id);
            _productTypes.Add(productType.Id, cache);
        }
    }

    public ProductType? GetProductType(string name)
    {
        lock (_productTypes)
        lock (_productTypesByName)
        {
            bool found = _productTypesByName.TryGetValue(name, out int id);
            if (found) return _productTypes[id];
            
            ProductType? productType = new CentralContext().ProductTypes.SingleOrDefault(type => type.Name == name);
            if (productType == null) return null;
            
            CacheProductType(productType);
            return _productTypes[productType.Id];
        }
    }
    
    public ProductType? GetProductType(int id)
    {
        lock (_productTypes)
        {
            bool found = _productTypes.TryGetValue(id, out ProductType? productType);
            if (found) return productType;
            
            productType = new CentralContext().ProductTypes.SingleOrDefault(type => type.Id == id);
            if (productType == null) return null;
            
            CacheProductType(productType);
            return _productTypes[productType.Id];
        }
    }

    public ProductType? AddProductType(string name, int id = -1)
    {
        if (id == -1)
        {
            try
            {
                id = new CentralContext().ProductTypes.Max(type => type.Id) + 1;
            }
            catch (InvalidOperationException)
            {
                id = 1;
            }
        }

        ProductType productType = new ProductType(name, id, 1);
        using var context = new CentralContext();
        context.ProductTypes.Add(productType);
        try
        {
            context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            return null;
        }
        
        CacheProductType(productType);
        return productType;
    } 
}