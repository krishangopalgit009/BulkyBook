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
    public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
    {
        private readonly ApplicationDBContext _dBConext;

        public OrderDetailRepository(ApplicationDBContext DBConext) : base(DBConext)
        {
            _dBConext = DBConext;
        }

        public void Update(OrderDetail orderDetail)
        {
            _dBConext.Update(orderDetail);  
        }
    }
}
