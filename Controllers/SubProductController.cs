using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grevity.Services.Interfaces;
using Grevity.Models.Entities;

namespace Grevity.Controllers
{
    [Authorize]
    public class SubProductController : Controller
    {
        private readonly ISubProductService _subProductService;

        public SubProductController(ISubProductService subProductService)
        {
            _subProductService = subProductService;
        }

        public async Task<IActionResult> Index()
        {
            var subProducts = await _subProductService.GetAllSubProductsAsync();
            return View(subProducts);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(SubProduct subProduct)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _subProductService.AddSubProductAsync(subProduct);
                    TempData["Success"] = $"Sub Product '{subProduct.Name}' created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating Sub Product: " + ex.Message);
                }
            }
            return View(subProduct);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var subProduct = await _subProductService.GetSubProductByIdAsync(id);
            if (subProduct == null)
            {
                return NotFound();
            }
            return View(subProduct);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SubProduct subProduct)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _subProductService.UpdateSubProductAsync(subProduct);
                    TempData["Success"] = $"Sub Product '{subProduct.Name}' updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating Sub Product: " + ex.Message);
                }
            }
            return View(subProduct);
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _subProductService.DeleteSubProductAsync(id);
                TempData["Success"] = "Sub Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetSubProductDetails(int id)
        {
            var subProduct = await _subProductService.GetSubProductByIdAsync(id);
            if (subProduct == null)
            {
                return NotFound();
            }
            return Json(subProduct);
        }
    }
}
