using Microsoft.AspNetCore.Mvc.Rendering;
using MyProject.Models;
using MyProject.Repositories;

namespace MyProject.Services;

public interface ITableNameService
{
    Task<(bool Success, string Message)> SaveAsync(TableNameModel model, int userId);
    Task<(bool Success, string Message)> DeleteAsync(int id, int userId);
    Task<TableNameModel?> GetByIdAsync(int id);
    Task<List<TableNameModel>> SearchAsync(int? groupCompanyId, int? plantId, string? fieldOne);
    Task<List<SelectListItem>> GetGroupCompanyDropdownAsync();
    Task<List<SelectListItem>> GetPlantDropdownAsync(int? groupCompanyId);
    Task<List<SelectListItem>> GetSupplierDropdownAsync();
    Task<List<SelectListItem>> GetCurrencyDropdownAsync();
    Task<Dictionary<string, string>> GetPlantCountryMapAsync(int? groupCompanyId);
}

public class TableNameService : ITableNameService
{
    private readonly ITableNameRepository _repository;

    public TableNameService(ITableNameRepository repository) => _repository = repository;

    public async Task<(bool Success, string Message)> SaveAsync(TableNameModel model, int userId)
    {
        if (model.EndDate <= model.StartDate) return (false, "End Date must be after Start Date.");
        if (model.Amount < 0) return (false, "Amount cannot be negative.");
        var result = model.RecordId == 0
            ? await _repository.InsertAsync(model, userId)
            : await _repository.UpdateAsync(model, userId);
        return (result.Result == 1, result.Result == 1 ? "Record saved successfully." : string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Save failed." : result.ErrorMessage);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, int userId)
    {
        var result = await _repository.DeleteAsync(id, userId);
        return (result.Result == 1, result.Result == 1 ? "Record deleted successfully." : "Delete failed.");
    }

    public Task<TableNameModel?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
    public Task<List<TableNameModel>> SearchAsync(int? groupCompanyId, int? plantId, string? fieldOne) => _repository.SearchAsync(groupCompanyId, plantId, fieldOne);
    public Task<List<SelectListItem>> GetGroupCompanyDropdownAsync() => _repository.GetGroupCompaniesAsync();
    public Task<List<SelectListItem>> GetPlantDropdownAsync(int? groupCompanyId) => _repository.GetPlantsAsync(groupCompanyId);
    public Task<List<SelectListItem>> GetSupplierDropdownAsync() => _repository.GetSuppliersAsync();
    public Task<List<SelectListItem>> GetCurrencyDropdownAsync() => _repository.GetCurrenciesAsync();
    public Task<Dictionary<string, string>> GetPlantCountryMapAsync(int? groupCompanyId) => _repository.GetPlantCountryMapAsync(groupCompanyId);
}
