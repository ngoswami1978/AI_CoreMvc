using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MyProject.Models;

public class TableNameModel
{
    public int RecordId { get; set; }

    [Required(ErrorMessage = "Group Company is required")]
    [Display(Name = "Group Company")]
    public int GroupCompanyId { get; set; }

    [Required(ErrorMessage = "Plant is required")]
    [Display(Name = "Plant")]
    public int PlantId { get; set; }

    [Required(ErrorMessage = "Supplier is required")]
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Currency is required")]
    public int CurrencyId { get; set; }

    [Required, StringLength(100)]
    public string FieldOne { get; set; } = string.Empty;

    [ValidateNever]
    [StringLength(250)]
    public string? FieldTwo { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Range(0, 999999999.99)]
    public decimal Amount { get; set; }

    [ValidateNever] public string? GroupCompanyName { get; set; }
    [ValidateNever] public string? PlantName { get; set; }
    [ValidateNever] public string? PlantCountry { get; set; }
    [ValidateNever] public string? SupplierName { get; set; }
    [ValidateNever] public string? CurrencyCode { get; set; }

    [ValidateNever] public int CUserId { get; set; }
    [ValidateNever] public DateTime CDatetime { get; set; }
    [ValidateNever] public int? MUserId { get; set; }
    [ValidateNever] public DateTime? MDatetime { get; set; }
    [ValidateNever] public bool IsDeleted { get; set; }
}
