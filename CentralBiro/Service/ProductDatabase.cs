using System;
using System.Collections.Generic;
using System.Linq;
using CentralBiro.Database;

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
            int nextSerialNumber;
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
            int id;
            bool found = _productTypesByName.TryGetValue(name, out id);
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
            ProductType? productType;
            bool found = _productTypes.TryGetValue(id, out productType);
            if (found) return productType;
            
            productType = new CentralContext().ProductTypes.SingleOrDefault(type => type.Id == id);
            if (productType == null) return null;
            
            CacheProductType(productType);
            return _productTypes[productType.Id];
        }
    }
}