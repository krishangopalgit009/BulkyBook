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
    public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
    {
        private readonly ApplicationDBContext _dBConext;

        public ShoppingCartRepository(ApplicationDBContext DBConext) : base(DBConext)
        {
            _dBConext = DBConext;
        }

        public int DecrementCount(ShoppingCart Shoppingcart, int count)
        {
            Shoppingcart.Count -= count;
            return Shoppingcart.Count;
        }

        public int IncrementCount(ShoppingCart Shoppingcart, int count)
        {
            Shoppingcart.Count += count;
            return Shoppingcart.Count;
        }
    }
}
