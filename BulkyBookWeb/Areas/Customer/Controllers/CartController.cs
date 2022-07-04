using BulkyBook.DataAccess.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;

        [BindProperty]
        public ShoppingCartVM _shoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
            Claim claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            _shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == claim.Value,
                includeProperties: "Product"),
                OrderHeader = new()
            };

            foreach (var item in _shoppingCartVM.ListCart)
            {
                item.Price = GetPriceBasedOnQty(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100);

                _shoppingCartVM.OrderHeader.OrderTotal += (item.Price * item.Count);
            }

            return View(_shoppingCartVM);
        }

        public IActionResult plus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCartRepo.GetFirstOrDefault(u => u.Id == cartId);
            if (cart != null)
            {
                _unitOfWork.ShoppingCartRepo.IncrementCount(cart, 1);
                _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult minus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCartRepo.GetFirstOrDefault(u => u.Id == cartId);
            if (cart != null)
            {
                if (cart.Count <= 1)
                {
                    _unitOfWork.ShoppingCartRepo.Remove(cart);

                    var count = _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;

                    if (count > 0)
                    {
                        count--;
                    }

                    HttpContext.Session.SetInt32(StaticDetail.SessionCart, count);
                }
                else
                {
                    _unitOfWork.ShoppingCartRepo.DecrementCount(cart, 1);
                }
                _unitOfWork.Save();
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cart = _unitOfWork.ShoppingCartRepo.GetFirstOrDefault(u => u.Id == cartId);
            if (cart != null)
            {
                _unitOfWork.ShoppingCartRepo.Remove(cart);
                _unitOfWork.Save();

                var count = _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count - 1;
                HttpContext.Session.SetInt32(StaticDetail.SessionCart, count);

            }
            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQty(double quantity, double price, double price50, double price100)
        {
            if (quantity <= 50)
            {
                return price;
            }
            else
            {
                if (quantity <= 100)
                {
                    return price50;
                }
                return price100;
            }
        }

        public IActionResult Summary()
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
            Claim claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            _shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product"),
                OrderHeader = new()
            };

            _shoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUserRepo.GetFirstOrDefault(u => u.Id == claim.Value);

            _shoppingCartVM.OrderHeader.Name = _shoppingCartVM.OrderHeader.ApplicationUser.Name;
            _shoppingCartVM.OrderHeader.StreetAddress = _shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            _shoppingCartVM.OrderHeader.City = _shoppingCartVM.OrderHeader.ApplicationUser.City;
            _shoppingCartVM.OrderHeader.State = _shoppingCartVM.OrderHeader.ApplicationUser.State;
            _shoppingCartVM.OrderHeader.PostalCode = _shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var item in _shoppingCartVM.ListCart)
            {
                item.Price = GetPriceBasedOnQty(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100);

                _shoppingCartVM.OrderHeader.OrderTotal += (item.Price * item.Count);
            }
            return View(_shoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPost()
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)User.Identity;
            Claim claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            _shoppingCartVM.ListCart = _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "Product");

            _shoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
            _shoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;

            foreach (var item in _shoppingCartVM.ListCart)
            {
                item.Price = GetPriceBasedOnQty(item.Count, item.Product.Price, item.Product.Price50, item.Product.Price100);

                _shoppingCartVM.OrderHeader.OrderTotal += (item.Price * item.Count);
            }

            ApplicationUser applicationUser = _unitOfWork.ApplicationUserRepo.GetFirstOrDefault(u => u.Id == claim.Value); //For Company employee*

            if (applicationUser.CompanyId.GetValueOrDefault()==0)
            {
                _shoppingCartVM.OrderHeader.PaymentStatus = StaticDetail.PaymentStatusPending;
                _shoppingCartVM.OrderHeader.OrderStatus = StaticDetail.StatusPending;
            }
            else
            {
                _shoppingCartVM.OrderHeader.PaymentStatus = StaticDetail.PaymentStatusDelayedPayment;
                _shoppingCartVM.OrderHeader.OrderStatus = StaticDetail.StatusApproved;
            }

            _unitOfWork.OrderHeaderRepo.Add(_shoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach (var item in _shoppingCartVM.ListCart)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = item.ProductId,
                    OrderId = _shoppingCartVM.OrderHeader.Id,
                    Price = item.Price,
                    Count = item.Count
                };
                _unitOfWork.OrderDetailRepo.Add(orderDetail);
                _unitOfWork.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
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
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={_shoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain + $"customer/cart/Index",
                };

                foreach (var item in _shoppingCartVM.ListCart)
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

                _unitOfWork.OrderHeaderRepo.UpdateStripePaymentId(_shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            else
            {
                return RedirectToAction("OrderConfirmation", "Cart", new { id = _shoppingCartVM.OrderHeader.Id });
            }
        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeaderRepo.GetFirstOrDefault(o => o.Id == id, includeProperties:"ApplicationUser");

            if (orderHeader.PaymentStatus!=StaticDetail.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeaderRepo.UpdateStatus(id, StaticDetail.StatusApproved, StaticDetail.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
            //send orderconfirmation email to customer**
            //_emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "Order Placed", "<p>Your Order placed Successfully</p>");

            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

            _unitOfWork.ShoppingCartRepo.RemoveRange(shoppingCarts);
            _unitOfWork.Save();

            return View(id);

        }

    }
}
