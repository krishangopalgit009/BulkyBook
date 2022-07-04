using BulkyBook.Data;
using BulkyBook.DataAccess.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = StaticDetail.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        //Get
        public IActionResult Create()
        {
            return View();
        }

        //Post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Company obj)
        {

            if (ModelState.IsValid)
            {
                _unitOfWork.CompanyRepo.Add(obj);
                _unitOfWork.Save();
                TempData["Success"] = "Company Created successfully";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        //Get
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var company = _unitOfWork.CompanyRepo.GetFirstOrDefault(u => u.Id == id);

            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        //Post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Company obj)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.CompanyRepo.Update(obj);
                _unitOfWork.Save();

                TempData["Success"] = "Company updated successfully";

                return RedirectToAction("Index");
            }
            return View(obj);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAllCompanies()
        {
            var cList = _unitOfWork.CompanyRepo.GetAll(includeProperties: null);
            return Json(new { data = cList });
        }

        //Post
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var company = _unitOfWork.CompanyRepo.GetFirstOrDefault(u => u.Id == id);

            if (company == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unitOfWork.CompanyRepo.Remove(company);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete successfull" });

        }
        #endregion
    }
}
