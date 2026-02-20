using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using MyProject.Models;

namespace MyProject.Repositories;

public interface ITableNameRepository
{
    Task<(int Result, string ErrorMessage)> InsertAsync(TableNameModel model, int userId);
    Task<(int Result, string ErrorMessage)> UpdateAsync(TableNameModel model, int userId);
    Task<(int Result, string ErrorMessage)> DeleteAsync(int id, int userId);
    Task<TableNameModel?> GetByIdAsync(int id);
    Task<List<TableNameModel>> SearchAsync(int? groupCompanyId, int? plantId, string? fieldOne);
    Task<List<SelectListItem>> GetGroupCompaniesAsync();
    Task<List<SelectListItem>> GetPlantsAsync(int? groupCompanyId);
    Task<List<SelectListItem>> GetSuppliersAsync();
    Task<List<SelectListItem>> GetCurrenciesAsync();
    Task<Dictionary<string, string>> GetPlantCountryMapAsync(int? groupCompanyId);
}

public class TableNameRepository : ITableNameRepository
{
    private readonly string _connectionString;

    public TableNameRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<(int Result, string ErrorMessage)> InsertAsync(TableNameModel model, int userId)
        => await ExecSaveAsync("usp_TableName_INSERT", model, userId);

    public async Task<(int Result, string ErrorMessage)> UpdateAsync(TableNameModel model, int userId)
        => await ExecSaveAsync("usp_TableName_UPDATE", model, userId, true);

    private async Task<(int Result, string ErrorMessage)> ExecSaveAsync(string sp, TableNameModel model, int userId, bool includeId = false)
    {
        using var con = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sp, con) { CommandType = CommandType.StoredProcedure };
        if (includeId) cmd.Parameters.AddWithValue("@RecordId", model.RecordId);
        cmd.Parameters.AddWithValue("@GroupCompanyId", model.GroupCompanyId);
        cmd.Parameters.AddWithValue("@PlantId", model.PlantId);
        cmd.Parameters.AddWithValue("@SupplierId", model.SupplierId);
        cmd.Parameters.AddWithValue("@CurrencyId", model.CurrencyId);
        cmd.Parameters.AddWithValue("@FieldOne", model.FieldOne);
        cmd.Parameters.AddWithValue("@FieldTwo", (object?)model.FieldTwo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StartDate", model.StartDate);
        cmd.Parameters.AddWithValue("@EndDate", model.EndDate);
        cmd.Parameters.AddWithValue("@Amount", model.Amount);
        cmd.Parameters.AddWithValue("@UserId", userId);
        var result = new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var error = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(result); cmd.Parameters.Add(error);
        await con.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
        return (Convert.ToInt32(result.Value), Convert.ToString(error.Value) ?? string.Empty);
    }

    public async Task<(int Result, string ErrorMessage)> DeleteAsync(int id, int userId)
    {
        using var con = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_TableName_DELETE", con) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@RecordId", id);
        cmd.Parameters.AddWithValue("@UserId", userId);
        var result = new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var error = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(result); cmd.Parameters.Add(error);
        await con.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
        return (Convert.ToInt32(result.Value), Convert.ToString(error.Value) ?? string.Empty);
    }

    public async Task<TableNameModel?> GetByIdAsync(int id)
    {
        using var con = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_TableName_GETBYID", con) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@RecordId", id);
        await con.OpenAsync();
        using var dr = await cmd.ExecuteReaderAsync();
        return await dr.ReadAsync() ? Map(dr) : null;
    }

    public async Task<List<TableNameModel>> SearchAsync(int? groupCompanyId, int? plantId, string? fieldOne)
    {
        var list = new List<TableNameModel>();
        using var con = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_TableName_SEARCH", con) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@GroupCompanyId", (object?)groupCompanyId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PlantId", (object?)plantId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FieldOne", (object?)fieldOne ?? DBNull.Value);
        await con.OpenAsync();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync()) list.Add(Map(dr));
        return list;
    }

    public Task<List<SelectListItem>> GetGroupCompaniesAsync() => Lookup("usp_Lookup_GroupCompany");
    public Task<List<SelectListItem>> GetSuppliersAsync() => Lookup("usp_Lookup_Supplier");
    public Task<List<SelectListItem>> GetCurrenciesAsync() => Lookup("usp_Lookup_Currency");

    public Task<List<SelectListItem>> GetPlantsAsync(int? groupCompanyId)
        => Lookup("usp_Lookup_Plant", new SqlParameter("@GroupCompanyId", (object?)groupCompanyId ?? DBNull.Value));

    public async Task<Dictionary<string, string>> GetPlantCountryMapAsync(int? groupCompanyId)
    {
        var map = new Dictionary<string, string>();
        using var con = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("usp_Lookup_Plant", con) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@GroupCompanyId", (object?)groupCompanyId ?? DBNull.Value);
        await con.OpenAsync();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync()) map[dr["Value"].ToString() ?? ""] = dr["Country"].ToString() ?? "";
        return map;
    }

    private async Task<List<SelectListItem>> Lookup(string sp, params SqlParameter[] pars)
    {
        var list = new List<SelectListItem> { new() { Value = "", Text = "Select" } };
        using var con = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sp, con) { CommandType = CommandType.StoredProcedure };
        if (pars.Any()) cmd.Parameters.AddRange(pars);
        await con.OpenAsync();
        using var dr = await cmd.ExecuteReaderAsync();
        while (await dr.ReadAsync()) list.Add(new SelectListItem { Value = dr["Value"].ToString(), Text = dr["Text"].ToString() });
        return list;
    }

    private static TableNameModel Map(SqlDataReader dr) => new()
    {
        RecordId = Convert.ToInt32(dr["RecordId"]),
        GroupCompanyId = Convert.ToInt32(dr["GroupCompanyId"]),
        PlantId = Convert.ToInt32(dr["PlantId"]),
        SupplierId = Convert.ToInt32(dr["SupplierId"]),
        CurrencyId = Convert.ToInt32(dr["CurrencyId"]),
        FieldOne = dr["FieldOne"].ToString() ?? string.Empty,
        FieldTwo = dr.HasColumn("FieldTwo") ? dr["FieldTwo"].ToString() : null,
        StartDate = Convert.ToDateTime(dr["StartDate"]),
        EndDate = Convert.ToDateTime(dr["EndDate"]),
        Amount = Convert.ToDecimal(dr["Amount"]),
        GroupCompanyName = dr.HasColumn("GroupCompanyName") ? dr["GroupCompanyName"].ToString() : null,
        PlantName = dr.HasColumn("PlantName") ? dr["PlantName"].ToString() : null,
        PlantCountry = dr.HasColumn("PlantCountry") ? dr["PlantCountry"].ToString() : null,
        SupplierName = dr.HasColumn("SupplierName") ? dr["SupplierName"].ToString() : null,
        CurrencyCode = dr.HasColumn("CurrencyCode") ? dr["CurrencyCode"].ToString() : null
    };
}

public static class DataReaderExtensions
{
    public static bool HasColumn(this SqlDataReader dr, string column)
    {
        for (var i = 0; i < dr.FieldCount; i++) if (dr.GetName(i).Equals(column, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
