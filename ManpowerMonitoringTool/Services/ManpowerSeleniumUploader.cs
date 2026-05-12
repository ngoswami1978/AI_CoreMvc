using ManpowerMonitoringTool.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ManpowerMonitoringTool.Services;

public sealed class ManpowerSeleniumUploader : IDisposable
{
    private readonly BrowserAutomationOptions _options;
    private readonly Action<string> _log;
    private IWebDriver? _driver;
    private WebDriverWait? _wait;

    public ManpowerSeleniumUploader(BrowserAutomationOptions options, Action<string> log)
    {
        _options = options;
        _log = log;
    }

    public void StartBrowser()
    {
        if (_driver != null)
        {
            return;
        }

        var chromeOptions = new ChromeOptions();
        chromeOptions.AddArgument("--start-maximized");
        _driver = new ChromeDriver(chromeOptions);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));

        if (!string.IsNullOrWhiteSpace(_options.Url))
        {
            _driver.Navigate().GoToUrl(_options.Url);
        }

        _log("Chrome started. Login manually if the website asks for credentials, then click Run Upload.");
    }

    public void Upload(IReadOnlyList<ManpowerEntry> entries, CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            _log("No rows found in Excel.");
            return;
        }

        StartBrowser();
        foreach (var group in entries.GroupBy(x => new { x.UnitName, x.CurrentYear, x.CurrentMonth }))
        {
            cancellationToken.ThrowIfCancellationRequested();
            _log($"Opening unit {group.Key.UnitName}, {group.Key.CurrentMonth}/{group.Key.CurrentYear}");
            SelectPageContext(group.Key.UnitName, group.Key.CurrentYear, group.Key.CurrentMonth);

            foreach (var entry in group)
            {
                cancellationToken.ThrowIfCancellationRequested();
                FillFunctionRow(entry);
            }
        }

        _log("Upload completed. Review the browser page and save/submit from the website if required.");
    }

    private void SelectPageContext(string unitName, int year, int month)
    {
        SetFieldValue(_options.UnitSelector, unitName);
        SetFieldValue(_options.YearSelector, year.ToString(CultureInfo.InvariantCulture));
        SetFieldValue(
            _options.MonthSelector,
            month.ToString(CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
            CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month));

        if (!string.IsNullOrWhiteSpace(_options.SearchButtonSelector))
        {
            FindByCss(_options.SearchButtonSelector).Click();
            Thread.Sleep(750);
        }
    }

    private void FillFunctionRow(ManpowerEntry entry)
    {
        var row = FindFunctionRow(entry.Function);
        var inputs = row.FindElements(By.CssSelector("input, textarea")).Where(x => x.Displayed && x.Enabled).ToList();
        if (inputs.Count < 2)
        {
            _log($"Skipped {entry.UnitName} / {entry.Function}: row does not contain two editable amount boxes.");
            return;
        }

        SetElementValue(inputs[0], entry.ActualMpCost);
        SetElementValue(inputs[1], entry.ActualMpCostLeasing);
        _log($"Filled {entry.UnitName} / {entry.Function}: internal={entry.ActualMpCost}, leasing={entry.ActualMpCostLeasing}");
    }

    private IWebElement FindFunctionRow(string functionName)
    {
        var expected = Normalize(functionName);
        var table = FindByCss(_options.TableSelector);
        var rows = table.FindElements(By.CssSelector("tr"));
        foreach (var row in rows)
        {
            var cells = row.FindElements(By.CssSelector("td, th"));
            if (cells.Count == 0)
            {
                continue;
            }

            if (Normalize(cells[0].Text) == expected)
            {
                return row;
            }
        }

        throw new NoSuchElementException($"Could not find a table row for function '{functionName}'.");
    }

    private void SetFieldValue(string cssSelector, string value, params string[] alternateSelectTexts)
    {
        if (string.IsNullOrWhiteSpace(cssSelector))
        {
            return;
        }

        var element = FindByCss(cssSelector);
        if (element.TagName.Equals("select", StringComparison.OrdinalIgnoreCase))
        {
            var select = new SelectElement(element);
            TrySelect(select, [value, .. alternateSelectTexts]);
            return;
        }

        element.Clear();
        element.SendKeys(value);
        element.SendKeys(OpenQA.Selenium.Keys.Tab);
    }

    private static void TrySelect(SelectElement select, IReadOnlyList<string> values)
    {
        foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            try
            {
                select.SelectByValue(value);
                return;
            }
            catch (NoSuchElementException)
            {
                // Try visible text next.
            }

            try
            {
                select.SelectByText(value);
                return;
            }
            catch (NoSuchElementException)
            {
                // Try the next candidate.
            }
        }

        throw new NoSuchElementException($"Could not select any of these values: {string.Join(", ", values)}");
    }

    private void SetElementValue(IWebElement element, decimal value)
    {
        element.Click();
        element.SendKeys(OpenQA.Selenium.Keys.Control + "a");
        element.SendKeys(value.ToString("0.##", CultureInfo.InvariantCulture));
        element.SendKeys(OpenQA.Selenium.Keys.Tab);
    }

    private IWebElement FindByCss(string cssSelector)
    {
        if (_wait == null)
        {
            throw new InvalidOperationException("Browser has not been started.");
        }

        return _wait.Until(driver =>
        {
            var element = driver.FindElement(By.CssSelector(cssSelector));
            return element.Displayed ? element : null;
        });
    }

    private static string Normalize(string value)
    {
        return Regex.Replace(value, "[^a-zA-Z0-9]", string.Empty).ToLowerInvariant();
    }

    public void Dispose()
    {
        if (!_options.KeepBrowserOpen)
        {
            _driver?.Quit();
        }

        _driver?.Dispose();
    }
}
