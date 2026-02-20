using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using MyProject.Models;
using MyProject.Services;
using MyProject.ViewModels;

namespace MyProject.Controllers;

public class TableNameController : Controller
{
    private readonly ITableNameService _service;
    private readonly ILogger<TableNameController> _logger;

    public TableNameController(ITableNameService service, ILogger<TableNameController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var vm = new TableNameViewModel
            {
                GroupCompanyList = await _service.GetGroupCompanyDropdownAsync(),
                PlantList = await _service.GetPlantDropdownAsync(null),
                SupplierList = await _service.GetSupplierDropdownAsync(),
                CurrencyList = await _service.GetCurrencyDropdownAsync(),
                PlantCountryMap = await _service.GetPlantCountryMapAsync(null)
            };
            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load index");
            return View(new TableNameViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int? groupCompanyId, int? plantId, string? fieldOne)
    {
        try { return Json(new { success = true, message = "", data = await _service.SearchAsync(groupCompanyId, plantId, fieldOne) }); }
        catch (Exception ex) { _logger.LogError(ex, "GetAll failed"); return Json(new { success = false, message = "Failed to load records.", data = Array.Empty<object>() }); }
    }

    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var data = await _service.GetByIdAsync(id);
            return Json(new { success = data != null, message = data == null ? "Record not found." : "", data = data ?? new() });
        }
        catch (Exception ex) { _logger.LogError(ex, "GetById failed"); return Json(new { success = false, message = "Failed to fetch record.", data = new { } }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromBody] TableNameModel model)
    {
        try
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Invalid request.", data = new { } });
            var result = await _service.SaveAsync(model, 1);
            return Json(new { success = result.Success, message = result.Message, data = new { } });
        }
        catch (Exception ex) { _logger.LogError(ex, "Save failed"); return Json(new { success = false, message = "Failed to save record.", data = new { } }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromBody] int id)
    {
        try
        {
            var result = await _service.DeleteAsync(id, 1);
            return Json(new { success = result.Success, message = result.Message, data = new { } });
        }
        catch (Exception ex) { _logger.LogError(ex, "Delete failed"); return Json(new { success = false, message = "Delete failed.", data = new { } }); }
    }

    [HttpGet]
    public async Task<IActionResult> GetPlants(int? groupCompanyId)
    {
        var plants = await _service.GetPlantDropdownAsync(groupCompanyId);
        var countryMap = await _service.GetPlantCountryMapAsync(groupCompanyId);
        var result = plants.Where(x => x.Value != "").Select(x => new { value = x.Value, text = x.Text, country = countryMap.TryGetValue(x.Value!, out var c) ? c : "" });
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(int? groupCompanyId, int? plantId, string? fieldOne)
    {
        try
        {
            var data = await _service.SearchAsync(groupCompanyId, plantId, fieldOne);
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("TableName");
            ws.Cell(1, 1).Value = "Field One"; ws.Cell(1, 2).Value = "Group Company"; ws.Cell(1, 3).Value = "Plant"; ws.Cell(1, 4).Value = "Country";
            var row = 2;
            foreach (var x in data)
            {
                ws.Cell(row, 1).Value = x.FieldOne;
                ws.Cell(row, 2).Value = x.GroupCompanyName;
                ws.Cell(row, 3).Value = x.PlantName;
                ws.Cell(row, 4).Value = x.PlantCountry;
                row++;
            }
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TableName.xlsx");
        }
        catch (Exception ex) { _logger.LogError(ex, "Export failed"); return RedirectToAction(nameof(Index)); }
    }
}
