using BulkyBook.DataAccess.IRepository;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyBookWeb.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                if (HttpContext.Session.GetInt32(StaticDetail.SessionCart) !=null)
                {
                    return View(HttpContext.Session.GetInt32(StaticDetail.SessionCart));
                }
                else
                {
                    HttpContext.Session.SetInt32(StaticDetail.SessionCart,
                            _unitOfWork.ShoppingCartRepo.GetAll(u => u.ApplicationUserId == claim.Value).ToList().Count);

                    return View(HttpContext.Session.GetInt32(StaticDetail.SessionCart));
                }
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }
    }
}
