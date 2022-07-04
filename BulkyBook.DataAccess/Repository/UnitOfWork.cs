using BulkyBook.Data;
using BulkyBook.DataAccess.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDBContext _dBConext;

        public ICategoryRepository CategoryRepo { get; private set; }
        public ICoverTypeRepository CoverTypeRepo { get; private set; }
        public  IProductRepository ProductRepo { get; private set; }
        public ICompanyRepository CompanyRepo { get; private set; }
        public IShoppingCartRepository ShoppingCartRepo { get; private set; }
        public IApplicationUserRepository ApplicationUserRepo { get; private set; }
        public  IOrderHeaderRepository OrderHeaderRepo { get; private set; }
        public  IOrderDetailRepository OrderDetailRepo { get; private set; }
               

        public UnitOfWork (ApplicationDBContext DBConext)
        {
            _dBConext = DBConext;

            CategoryRepo = new CategoryRepository(_dBConext);
            CoverTypeRepo = new CoverTypeRepository(_dBConext);
            ProductRepo = new ProductRepository(_dBConext);
            CompanyRepo = new CompanyRepository(_dBConext);
            ShoppingCartRepo = new ShoppingCartRepository(_dBConext);
            ApplicationUserRepo = new ApplicationUserRepository(_dBConext);
            OrderHeaderRepo = new OrderHeaderRepsitory(_dBConext);
            OrderDetailRepo = new OrderDetailRepository(_dBConext);
        }

        public void Save()
        {
            _dBConext.SaveChanges();    
        }
    }
}
 