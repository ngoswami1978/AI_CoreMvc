# Manpower Cost Selenium Monitoring Tool

This Windows Forms application reads the manpower Excel sheet and uses Selenium to open a local Chrome browser. It searches the website by `UnitName`, year, and month, then fills the first two editable amount boxes in each matching function row:

- `ACTUAL_MP_COST` -> the table column for **Actual MP-Cost Internal / LC 000**
- `ACTUAL_MP_COST_LEASING` -> the table column for **Actual MP-Cost Leasing / LC 000**

## Excel format

The first worksheet must contain these headers. Spaces, underscores, and line breaks in the header names are accepted:

- `Unit code`
- `UnitName`
- `current_year`
- `current_month`
- `Function`
- `ACTUAL_MP_COST`
- `ACTUAL_MP_COST_LEASING`
- `TOTAL_MP_COST`
- `Section`

Rows without `UnitName` or `Function` are skipped.

## How to run on Windows

1. Install .NET 8 SDK and Google Chrome.
2. Open a terminal in this project folder.
3. Run `dotnet restore ManpowerMonitoringTool/ManpowerMonitoringTool.csproj`.
4. Run `dotnet run --project ManpowerMonitoringTool/ManpowerMonitoringTool.csproj`.
5. Choose the Excel file and click **Load Preview**.
6. Enter the website URL and CSS selectors for the unit, year, month, search button, and manpower cost table.
7. Click **Start Browser**. Log in manually if your website requires login.
8. Click **Run Upload** to populate the screen.

## Selector setup

The default selector values are examples only:

| UI field | Default | Purpose |
| --- | --- | --- |
| Website URL | `http://localhost:5000` | The page Selenium opens. |
| Unit selector | `#unit_list` | UnitName search/select control. |
| Year selector | `#SelectedYear` | Year input/select control. |
| Month selector | `#SelectedMonth` | Month input/select control. The tool writes the first three month characters, for example `Apr`, `May`, or `Jun`. |
| Search button | `#btnSearch` | Button clicked after unit/month/year are set. Leave blank if search is automatic. |
| Cost table | `table` | Table containing rows such as Finance, Quality, Manufacturing, and Maintenance. |

The automation matches the Excel `Function` value to the first cell of each table row after removing spaces, slash characters, and punctuation. This handles labels like `Manufacturing/  Operations` and `Manufacturing/ Operations`.

## Important safety note

The tool fills values in the browser but does not click the website's final Save/Submit button. Review the populated page before saving to production.
