using BulkyBook.DataAccess.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDataProtector _dataProtector;

        public HomeController(ILogger<HomeController> logger,IUnitOfWork unitOfWork, 
                IDataProtectionProvider dataProtectionProvider,
                DataProtectionPurposeStrings dataProtectionPurposeStrings)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _dataProtector = dataProtectionProvider.CreateProtector("ProductRouteIdValue");

        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _unitOfWork.ProductRepo.GetAll(includeProperties: "Category,CoverType")
                                                .Select(p => {
                                                            p.EncryptedId = _dataProtector.Protect(p.Id.ToString());
                                                            return p;
                                                });
            return View(productList);
        }

        public IActionResult Details(string EncryptedId)
        {

            int decryptedId = Convert.ToInt32(_dataProtector.Unprotect(EncryptedId));
                       
            ShoppingCart shoppingCart = new()
            {
                Count = 1,
                ProductId = decryptedId, 
                Product = _unitOfWork.ProductRepo.GetFirstOrDefault(p => p.Id == decryptedId, includeProperties: "Category,CoverType")
            };

            return View(shoppingCart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart _shoppingCart)
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            _shoppingCart.ApplicationUserId = claim.Value;

            ShoppingCart shoppingCartDb = _unitOfWork.ShoppingCartRepo.GetFirstOrDefault(u => u.ApplicationUserId == claim.Value &&
                                         u.ProductId == _shoppingCart.ProductId);

            if (shoppingCartDb == null)
            {
                _unitOfWork.ShoppingCartRepo.Add(_shoppingCart);
                _unitOfWork.Save();

                HttpContext.Session.SetInt32(StaticDetail.SessionCart, 
                    _unitOfWork.ShoppingCartRepo.GetAll(p=> p.ApplicationUserId == claim.Value).ToList().Count);
            }
            else
            {
                _unitOfWork.ShoppingCartRepo.IncrementCount(shoppingCartDb, _shoppingCart.Count);
                _unitOfWork.Save();
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}