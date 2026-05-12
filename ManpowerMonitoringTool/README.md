# Manpower Cost Selenium Monitoring Tool

This Windows Forms application reads the manpower Excel sheet and uses Selenium to open a local Chrome browser. It processes the preview grid one row at a time, selects month, year, and `UnitName`, clicks the Go/Search button, fills the first two editable amount boxes in the matching function row, then clicks the Save button:

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
6. Enter the website URL and CSS selectors for the unit, year, month, Go/Search button, Save button, and manpower cost table.
7. Click **Start Browser**. Log in manually if your website requires login.
8. Click **Run Upload**. The tool highlights each preview-grid row, waits 2 seconds before each entry action, enables any disabled month/year/unit dropdowns, selects that row's month/year/UnitName, clicks the Go/Search button, enters the manpower cost values, then clicks the Save button.

## Selector setup

The default selector values are examples only:

| UI field | Default | Purpose |
| --- | --- | --- |
| Website URL | `http://localhost:5000` | The page Selenium opens. |
| Unit selector | `#unit_list` | UnitName search/select control. |
| Year selector | `#SelectedYear` | Year input/select control. |
| Month selector | `#SelectedMonth` | Month input/select control. The tool writes the first three month characters, for example `Apr`, `May`, or `Jun`. |
| Go/Search button | `#btnSearch` | Button clicked automatically after unit/month/year are set. Leave blank if search is automatic. |
| Save button | `#convert_table_newid` | Button clicked automatically after the row manpower cost values are entered. Leave blank if you do not want auto-save. |
| Cost table | `#MANPOWERCOST_FUNCTIONWISE_TAB2` | Manpower cost table containing rows such as Finance, Quality, Manufacturing, and Maintenance. |

Before selecting month/year/unit values, the automation removes disabled/read-only attributes from those controls and their options so Selenium can complete the selection; it does not click the old cancel/revert button. The automation matches the Excel `Function` value to the first cell of each table row after removing spaces, slash characters, and punctuation. This handles labels like `Manufacturing/  Operations` and `Manufacturing/ Operations`.

## Important safety note

The tool fills values in the browser and clicks the configured Save button (`#convert_table_newid`) after each row. If the browser shows an alert such as `Want to Clear Data?`, the automation waits until you manually press the alert button, then continues. Review the site after upload to confirm all rows were saved correctly.
