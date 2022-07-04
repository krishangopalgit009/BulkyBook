using BulkyBook.DataAccess.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = StaticDetail.Role_Admin)]
    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CoverTypeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<CoverType> covertypeList = _unitOfWork.CoverTypeRepo.GetAll();

            return View(covertypeList);
        }

        //Get
        public IActionResult Create()
        {
            return View();
        }

        //Post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CoverType coverType)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.CoverTypeRepo.Add(coverType);
                _unitOfWork.Save();

                TempData["Success"] = "Cover Type Created Successfully";
                return RedirectToAction("Index");
            }
            return View(coverType);
        }

        //Get
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var coverType = _unitOfWork.CoverTypeRepo.GetFirstOrDefault(u => u.Id == id);
            if (coverType == null)
            {
                return NotFound();
            }

            return View(coverType);
        }

        //Post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CoverType coverType)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.CoverTypeRepo.Update(coverType);
                _unitOfWork.Save();

                TempData["Success"] = "Cover Type Updated Successfully";

                return RedirectToAction("Index");
            }
            return View(coverType);
        }

        //Get
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var coverType = _unitOfWork.CoverTypeRepo.GetFirstOrDefault(u => u.Id == id);

            if (coverType == null)
            {
                return NotFound();
            }

            return View(coverType);
        }

        //Post
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var covertype = _unitOfWork.CoverTypeRepo.GetFirstOrDefault(u => u.Id == id);

            if (covertype == null)
            {
                return NotFound();
            }

            _unitOfWork.CoverTypeRepo.Remove(covertype);
            _unitOfWork.Save();

            TempData["Success"] = "Cover Type Deleted Successfully";
            return RedirectToAction("Index");

        }
    }
}
