using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyProject.Models;

namespace MyProject.ViewModels;

public class TableNameViewModel
{
    public TableNameModel Form { get; set; } = new();
    public List<TableNameModel> Records { get; set; } = new();
    public List<SelectListItem> GroupCompanyList { get; set; } = new();
    public List<SelectListItem> PlantList { get; set; } = new();
    public List<SelectListItem> SupplierList { get; set; } = new();
    public List<SelectListItem> CurrencyList { get; set; } = new();

    [ValidateNever]
    public Dictionary<string, string> PlantCountryMap { get; set; } = new();
}
