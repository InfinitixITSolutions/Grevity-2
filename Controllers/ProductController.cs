using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grevity.Services.Interfaces;
using Grevity.Models.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace Grevity.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ISubProductService _subProductService;
        private readonly IBusinessSettingService _businessSettingService;
        private readonly ICompanyContext _companyContext;

        public ProductController(
            IProductService productService,
            ISubProductService subProductService,
            IBusinessSettingService businessSettingService,
            ICompanyContext companyContext)
        {
            _productService = productService;
            _subProductService = subProductService;
            _businessSettingService = businessSettingService;
            _companyContext = companyContext;
        }

        public async Task<IActionResult> Index(string searchTerm)
        {
            var products = await _productService.GetAllProductsAsync();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                products = products.Where(p => 
                    (p.Name != null && p.Name.ToLower().Contains(searchTerm)) || 
                    (p.HSN != null && p.HSN.Contains(searchTerm))
                ).ToList();
            }

            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        public async Task<IActionResult> Create()
        {
            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;
            ViewBag.DefaultGSTPercentage = settings?.DefaultGSTPercentage ?? 18.00m;
            ViewBag.SubProducts = await _subProductService.GetAllSubProductsAsync();

            var product = new Product 
            { 
                GSTPercentage = settings?.DefaultGSTPercentage ?? 18.00m 
            };
            
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, int[] subProductIds, decimal[] quantities)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _productService.AddProductAsync(product);

                    if (subProductIds != null && subProductIds.Length > 0)
                    {
                        var mappings = subProductIds.Select((id, index) => new ProductSubProductMapping
                        {
                            ProductId = product.Id,
                            SubProductId = id,
                            RequiredQuantity = quantities[index],
                            CompanyId = product.CompanyId
                        }).ToList();

                        await _productService.UpdateProductCompositionAsync(product.Id, mappings);
                    }

                    TempData["Success"] = "Product created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error saving product: " + ex.Message;
                }
            }
            
            ViewBag.SubProducts = await _subProductService.GetAllSubProductsAsync();
            return View(product);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            var companyId = _companyContext.CurrentCompanyId;
            var settings = await _businessSettingService.GetSettingsAsync(companyId);
            
            ViewBag.IsGSTEnabled = settings?.IsGSTEnabled ?? true;
            ViewBag.SubProducts = await _subProductService.GetAllSubProductsAsync();
            
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Product product, int[] subProductIds, decimal[] quantities)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _productService.UpdateProductAsync(product);

                    var mappings = new List<ProductSubProductMapping>();
                    if (subProductIds != null)
                    {
                        mappings = subProductIds.Select((spId, index) => new ProductSubProductMapping
                        {
                            ProductId = id,
                            SubProductId = spId,
                            RequiredQuantity = quantities[index],
                            CompanyId = product.CompanyId
                        }).ToList();
                    }
                    
                    await _productService.UpdateProductCompositionAsync(id, mappings);

                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error updating product: " + ex.Message;
                }
            }

            ViewBag.SubProducts = await _subProductService.GetAllSubProductsAsync();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try 
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product != null)
                {
                    // Composition mappings are automatically handled if cascaded or manually in service
                    await _productService.DeleteProductAsync(id);
                    TempData["Success"] = "Product deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting product: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetComposition(int id)
        {
            var composition = await _productService.GetProductCompositionAsync(id);
            var result = composition.Select(m => new
            {
                subProductName = m.SubProduct.Name,
                unit = m.SubProduct.Unit,
                requiredQuantity = m.RequiredQuantity,
                currentStock = m.SubProduct.CurrentStock
            });
            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> CheckStockAvailability(int id, decimal quantity)
        {
            var isAvailable = await _productService.ValidateStockForSaleAsync(id, quantity);
            var message = await _productService.GetStockValidationMessageAsync(id, quantity);
            return Json(new { available = isAvailable, message = message });
        }
    }
}
