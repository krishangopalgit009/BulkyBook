using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.IRepository
{
    public interface IUnitOfWork
    {
        ICategoryRepository CategoryRepo { get; }
        ICoverTypeRepository CoverTypeRepo { get; }
        IProductRepository ProductRepo { get; }
        ICompanyRepository CompanyRepo { get; }
        IShoppingCartRepository ShoppingCartRepo { get; }
        IApplicationUserRepository ApplicationUserRepo { get; }
        IOrderHeaderRepository OrderHeaderRepo { get; }
        IOrderDetailRepository OrderDetailRepo { get; }

        void Save();
    }
}
