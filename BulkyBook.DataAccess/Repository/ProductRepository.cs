using BulkyBook.Data;
using BulkyBook.DataAccess.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class ProductRepository : Repository<Product> , IProductRepository
    {
        private readonly ApplicationDBContext _dBConext;

        public ProductRepository(ApplicationDBContext DBConext) : base(DBConext)
        {
            _dBConext = DBConext;
        }

        public void Update(Product product)
        {
            //For Single Record Update

            var productFromdb = _dBConext.Products.FirstOrDefault(u => u.Id == product.Id);
            if (productFromdb != null)
            {
                productFromdb.Title = product.Title;
                productFromdb.ISBN = product.ISBN;
                productFromdb.Price = product.Price;
                productFromdb.Price50 = product.Price50;
                productFromdb.ListPrice = product.ListPrice;
                productFromdb.Price100 = product.Price100;
                productFromdb.Description = product.Description;
                productFromdb.CategoryId = product.CategoryId;
                productFromdb.Author = product.Author;
                productFromdb.CoverTypeId = product.CoverTypeId;

                if (product.ImageUrl !=null)
                { 
                    productFromdb.ImageUrl = product.ImageUrl;
                }
            }
        }
    }
}
