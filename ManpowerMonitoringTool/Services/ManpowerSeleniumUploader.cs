using ManpowerMonitoringTool.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Globalization;
using System.Text.RegularExpressions;
using SeleniumKeys = OpenQA.Selenium.Keys;

namespace ManpowerMonitoringTool.Services;

public sealed class ManpowerSeleniumUploader : IDisposable
{
    private const int EntryDelayMilliseconds = 2000;

    private readonly BrowserAutomationOptions _options;
    private readonly Action<string> _log;
    private readonly Action<ManpowerEntry>? _selectGridEntry;
    private IWebDriver? _driver;
    private WebDriverWait? _wait;

    public ManpowerSeleniumUploader(BrowserAutomationOptions options, Action<string> log, Action<ManpowerEntry>? selectGridEntry = null)
    {
        _options = options;
        _log = log;
        _selectGridEntry = selectGridEntry;
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
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _selectGridEntry?.Invoke(entry);
            _log($"Processing grid row: {entry.UnitName}, {entry.CurrentMonth}/{entry.CurrentYear}, {entry.Function}");
            WaitBeforeEntry("Starting row entry");
            WaitForManualAlertToClose(cancellationToken);
            SelectPageContext(entry.UnitName, entry.CurrentYear, entry.CurrentMonth);
            if (FillFunctionRow(entry))
            {
                ClickSaveButtonIfConfigured();
            }
        }

        _log("Upload completed. Configured save button was clicked after each filled row.");
    }

    private void SelectPageContext(string unitName, int year, int month)
    {
        EnableSelectionControls();

        var monthAbbreviation = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month);
        SetFieldValue(
            _options.MonthSelector,
            monthAbbreviation,
            CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
            month.ToString(CultureInfo.InvariantCulture));
        SetFieldValue(_options.YearSelector, year.ToString(CultureInfo.InvariantCulture));
        SetFieldValue(_options.UnitSelector, unitName);

        if (!string.IsNullOrWhiteSpace(_options.SearchButtonSelector))
        {
            ClickByCss(_options.SearchButtonSelector);
            WaitForManualAlertToClose();
            Thread.Sleep(750);
        }
    }


    private void EnableSelectionControls()
    {
        EnableField(_options.MonthSelector);
        EnableField(_options.YearSelector);
        EnableField(_options.UnitSelector);
    }


    private void ClickSaveButtonIfConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.SaveButtonSelector))
        {
            return;
        }

        WaitBeforeEntry("Clicking save button");
        _log("Clicking save button after entering manpower cost values.");
        ClickByCss(_options.SaveButtonSelector);
        WaitForManualAlertToClose();
        Thread.Sleep(750);
    }

    private bool FillFunctionRow(ManpowerEntry entry)
    {
        var row = FindFunctionRow(entry.Function);
        var inputs = row.FindElements(By.CssSelector("input, textarea")).Where(x => x.Displayed && x.Enabled).ToList();
        if (inputs.Count < 2)
        {
            _log($"Skipped {entry.UnitName} / {entry.Function}: row does not contain two editable amount boxes.");
            return false;
        }

        SetElementValue(inputs[0], entry.ActualMpCost);
        SetElementValue(inputs[1], entry.ActualMpCostLeasing);
        _log($"Filled {entry.UnitName} / {entry.Function}: internal={entry.ActualMpCost}, leasing={entry.ActualMpCostLeasing}");
        return true;
    }

    private IWebElement FindFunctionRow(string functionName)
    {
        var expected = Normalize(functionName);
        var table = FindByCss(_options.TableSelector);
        WaitForManualAlertToClose();
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

        while (true)
        {
            try
            {
                EnableField(cssSelector);
                WaitBeforeEntry($"Entering value '{value}'");
                var element = FindByCss(cssSelector);
                if (element.TagName.Equals("select", StringComparison.OrdinalIgnoreCase))
                {
                    var select = new SelectElement(element);
                    TrySelect(select, [value, .. alternateSelectTexts]);
                    return;
                }

                element.Clear();
                element.SendKeys(value);
                element.SendKeys(SeleniumKeys.Tab);
                return;
            }
            catch (WebDriverException ex) when (IsUnexpectedAlertOpen(ex))
            {
                WaitForManualAlertToClose();
            }
        }
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
            catch (Exception ex) when (ex is NoSuchElementException or InvalidOperationException)
            {
                // Try visible text next.
            }

            try
            {
                select.SelectByText(value);
                return;
            }
            catch (Exception ex) when (ex is NoSuchElementException or InvalidOperationException)
            {
                // Try the next candidate.
            }
        }

        throw new NoSuchElementException($"Could not select any of these values: {string.Join(", ", values)}");
    }

    private void SetElementValue(IWebElement element, decimal value)
    {
        while (true)
        {
            try
            {
                WaitForManualAlertToClose();
                EnableElement(element);
                WaitBeforeEntry("Entering manpower cost");
                element.Click();
                element.SendKeys(SeleniumKeys.Control + "a");
                element.SendKeys(value.ToString("0.##", CultureInfo.InvariantCulture));
                element.SendKeys(SeleniumKeys.Tab);
                return;
            }
            catch (WebDriverException ex) when (IsUnexpectedAlertOpen(ex))
            {
                WaitForManualAlertToClose();
            }
        }
    }


    private void EnableField(string cssSelector)
    {
        if (string.IsNullOrWhiteSpace(cssSelector) || _driver is not IJavaScriptExecutor js)
        {
            return;
        }

        while (true)
        {
            try
            {
                WaitForManualAlertToClose();
                js.ExecuteScript(
                    "const element = document.querySelector(arguments[0]);"
                    + "if (!element) return;"
                    + "element.disabled = false;"
                    + "element.readOnly = false;"
                    + "element.removeAttribute('disabled');"
                    + "element.removeAttribute('readonly');"
                    + "element.classList.remove('disabled');"
                    + "if (element.tagName && element.tagName.toLowerCase() === 'select') {"
                    + "  Array.from(element.options).forEach(option => { option.disabled = false; option.removeAttribute('disabled'); });"
                    + "}"
                    + "element.dispatchEvent(new Event('change', { bubbles: true }));",
                    cssSelector);
                return;
            }
            catch (WebDriverException ex) when (IsUnexpectedAlertOpen(ex))
            {
                WaitForManualAlertToClose();
            }
        }
    }

    private void EnableElement(IWebElement element)
    {
        if (_driver is not IJavaScriptExecutor js)
        {
            return;
        }

        while (true)
        {
            try
            {
                WaitForManualAlertToClose();
                js.ExecuteScript(
                    "arguments[0].disabled = false;"
                    + "arguments[0].readOnly = false;"
                    + "arguments[0].removeAttribute('disabled');"
                    + "arguments[0].removeAttribute('readonly');"
                    + "arguments[0].classList.remove('disabled');",
                    element);
                return;
            }
            catch (WebDriverException ex) when (IsUnexpectedAlertOpen(ex))
            {
                WaitForManualAlertToClose();
            }
        }
    }

    private void WaitBeforeEntry(string action)
    {
        _log($"{action}. Waiting {EntryDelayMilliseconds / 1000} seconds before continuing.");
        Thread.Sleep(EntryDelayMilliseconds);
    }

    private void ClickByCss(string cssSelector)
    {
        while (true)
        {
            try
            {
                FindByCss(cssSelector).Click();
                return;
            }
            catch (WebDriverException ex) when (IsUnexpectedAlertOpen(ex))
            {
                WaitForManualAlertToClose();
            }
        }
    }

    private IWebElement FindByCss(string cssSelector)
    {
        if (_wait == null)
        {
            throw new InvalidOperationException("Browser has not been started.");
        }

        while (true)
        {
            WaitForManualAlertToClose();

            try
            {
                return _wait.Until(driver =>
                {
                    var element = driver.FindElement(By.CssSelector(cssSelector));
                    return element.Displayed ? element : null;
                });
            }
            catch (WebDriverException ex) when (IsUnexpectedAlertOpen(ex))
            {
                WaitForManualAlertToClose();
            }
        }
    }

    private void WaitForManualAlertToClose(CancellationToken cancellationToken = default)
    {
        if (_driver == null)
        {
            return;
        }

        var logged = false;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var alert = _driver.SwitchTo().Alert();
                if (!logged)
                {
                    _log($"Browser alert is open: '{alert.Text}'. Please close it manually to continue.");
                    logged = true;
                }

                Thread.Sleep(1000);
            }
            catch (NoAlertPresentException)
            {
                if (logged)
                {
                    _log("Browser alert closed. Continuing automation.");
                }

                return;
            }
            catch (WebDriverException ex) when (IsUnexpectedAlertOpen(ex))
            {
                if (!logged)
                {
                    _log("Browser alert is open. Please close it manually to continue.");
                    logged = true;
                }

                Thread.Sleep(1000);
            }
        }
    }

    private static bool IsUnexpectedAlertOpen(WebDriverException exception)
    {
        return exception.Message.Contains("unexpected alert open", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("modal dialog", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("user prompt", StringComparison.OrdinalIgnoreCase);
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
