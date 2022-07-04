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
    public class OrderHeaderRepsitory : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDBContext _dBConext;

        public OrderHeaderRepsitory(ApplicationDBContext DBConext) : base(DBConext)
        {
            _dBConext = DBConext;
        }

        public void Update(OrderHeader orderHeader)
        {
            _dBConext.Update(orderHeader);
        }

        public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
        {
            var order = _dBConext.OrderHeaders.FirstOrDefault(o => o.Id == id);
            
            if (order !=null)
            {
                order.OrderStatus=orderStatus;
                if (paymentStatus !=null)
                {
                    order.PaymentStatus=paymentStatus;
                }
            }
        }

        public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId)
        {
            var order = _dBConext.OrderHeaders.FirstOrDefault(o => o.Id == id);

            if (order != null)
            {
                order.SessionId = sessionId;
                order.PaymentDate = DateTime.Now;
                order.PaymentIntentId=paymentIntentId;
            }
        }
    }
}
