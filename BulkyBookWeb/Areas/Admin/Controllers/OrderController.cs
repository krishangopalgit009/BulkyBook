using BulkyBook.DataAccess.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public OrderVM _orderVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            _orderVM = new OrderVM()
            {
                OrderHeader = _unitOfWork.OrderHeaderRepo.GetFirstOrDefault(o => o.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetailRepo.GetAll(o => o.Id == orderId, includeProperties: "Product"),

            };

            return View(_orderVM);
        }

        [HttpPost]
        [ActionName("Details")]
        [ValidateAntiForgeryToken]
        public IActionResult DetailsPayNow() //Payment for employee who make payment after few days or a month
        {
            _orderVM.OrderHeader = _unitOfWork.OrderHeaderRepo.GetFirstOrDefault(o => o.Id == _orderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            _orderVM.OrderDetail = _unitOfWork.OrderDetailRepo.GetAll(o => o.Id == _orderVM.OrderHeader.Id, includeProperties: "Product");

            //Stripe
            var domain = "https://localhost:44366/";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                    {
                        "card",
                    },
                LineItems = new List<SessionLineItemOptions>(),

                Mode = "payment",
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderId={_orderVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={_orderVM.OrderHeader.Id}",
            };

            foreach (var item in _orderVM.OrderDetail)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        },
                    },
                    Quantity = item.Count,
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            _unitOfWork.OrderHeaderRepo.UpdateStripePaymentId(_orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);

        }

        public IActionResult PaymentConfirmation(int orderId)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeaderRepo.GetFirstOrDefault(o => o.Id == orderId);

            if (orderHeader.PaymentStatus == StaticDetail.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeaderRepo.UpdateStatus(orderId, orderHeader.OrderStatus, StaticDetail.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
            return View(orderId);
        }

        [HttpPost]
        [Authorize(Roles = StaticDetail.Role_Admin + "," + StaticDetail.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderDetail()
        {
                var _orderHeaderDB = _unitOfWork.OrderHeaderRepo.GetFirstOrDefault(o => o.Id == _orderVM.OrderHeader.Id,tracked:false);

                _orderHeaderDB.Name = _orderVM.OrderHeader.Name;
                _orderHeaderDB.PhoneNumber = _orderVM.OrderHeader.PhoneNumber;
                _orderHeaderDB.StreetAddress = _orderVM.OrderHeader.StreetAddress;
                _orderHeaderDB.City = _orderVM.OrderHeader.City;
                _orderHeaderDB.State = _orderVM.OrderHeader.State;
                _orderHeaderDB.PostalCode = _orderVM.OrderHeader.PostalCode;
                if (_orderVM.OrderHeader.Carrier != null)
                {
                 _orderHeaderDB.Carrier = _orderVM.OrderHeader.Carrier;
                }
                if (_orderVM.OrderHeader.TrackingNumber != null)
                {
                    _orderHeaderDB.TrackingNumber = _orderVM.OrderHeader.TrackingNumber;
                }
                _unitOfWork.OrderHeaderRepo.Update(_orderHeaderDB);
                _unitOfWork.Save();
            
                TempData["Success"] = "Order Details Updated Successfully.";
                return RedirectToAction("Details", "Order", new { orderId = _orderHeaderDB.Id });
        }

        [HttpPost]
        [Authorize(Roles = StaticDetail.Role_Admin + "," + StaticDetail.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult StartProcessing()
        {

            _unitOfWork.OrderHeaderRepo.UpdateStatus(_orderVM.OrderHeader.Id, StaticDetail.StatusInProcess);
            _unitOfWork.Save();

            TempData["Success"] = "Order Processed  Successfully.";
            return RedirectToAction("Details", "Order", new { orderId = _orderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = StaticDetail.Role_Admin + "," + StaticDetail.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult ShipOrder()
        {
            var _orderHeaderDB = _unitOfWork.OrderHeaderRepo.GetFirstOrDefault(o => o.Id == _orderVM.OrderHeader.Id, tracked: false);

            if (_orderHeaderDB != null)
            {
                _orderHeaderDB.TrackingNumber = _orderVM.OrderHeader.TrackingNumber;
                _orderHeaderDB.Carrier = _orderVM.OrderHeader.Carrier;
                _orderHeaderDB.OrderStatus = StaticDetail.StatusShipped;
                _orderHeaderDB.ShippingDate = DateTime.Now;

                if (_orderHeaderDB.PaymentStatus == StaticDetail.PaymentStatusDelayedPayment)
                {
                    _orderHeaderDB.PaymentDueDate = DateTime.Now.AddDays(30);
                }
            }

            _unitOfWork.OrderHeaderRepo.Update(_orderHeaderDB);
            _unitOfWork.Save();

            TempData["Success"] = "Order Shipped  Successfully.";
            return RedirectToAction("Details", "Order", new { orderId = _orderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = StaticDetail.Role_Admin + "," + StaticDetail.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder()
        {
            var _orderHeaderDB = _unitOfWork.OrderHeaderRepo.GetFirstOrDefault(o => o.Id == _orderVM.OrderHeader.Id, tracked: false);

            if (_orderHeaderDB.PaymentStatus==StaticDetail.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = _orderHeaderDB.PaymentIntentId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeaderRepo.UpdateStatus(_orderVM.OrderHeader.Id, StaticDetail.StatusCancelled,StaticDetail.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeaderRepo.UpdateStatus(_orderVM.OrderHeader.Id, StaticDetail.StatusCancelled, StaticDetail.StatusCancelled);
            }
            _unitOfWork.Save();

            TempData["Success"] = "Order Cancelled  Successfully.";
            return RedirectToAction("Details", "Order", new { orderId = _orderVM.OrderHeader.Id });
        }

        #region API Call
        [HttpGet]
        public IActionResult GetOrder(string status)
        {
            IEnumerable<OrderHeader> _orderHeaders;

            if (User.IsInRole(StaticDetail.Role_Admin) || User.IsInRole(StaticDetail.Role_Employee))
            {
                _orderHeaders = _unitOfWork.OrderHeaderRepo.GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                ClaimsIdentity? claimsIdentity = (ClaimsIdentity)User.Identity;
                Claim claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

                _orderHeaders = _unitOfWork.OrderHeaderRepo.GetAll(u=> u.ApplicationUserId == claim!.Value, includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    _orderHeaders= _orderHeaders.Where(u => u.PaymentStatus == StaticDetail.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    _orderHeaders = _orderHeaders.Where(u => u.OrderStatus == StaticDetail.StatusInProcess);
                    break;
                case "completed":
                    _orderHeaders = _orderHeaders.Where(u => u.OrderStatus == StaticDetail.StatusShipped);
                    break;
                case "approved":
                    _orderHeaders = _orderHeaders.Where(u => u.OrderStatus == StaticDetail.StatusApproved);
                    break;
                default:
                    break;
            }
            return Json(new { data = _orderHeaders });
        }
        #endregion

    }
}
