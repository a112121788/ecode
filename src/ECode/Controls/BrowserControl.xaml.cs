using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace ECode.Controls;

/// <summary>
/// 基于 WebView2 的应用内浏览器控件。提供工具栏（后退/前进/刷新/地址栏），
/// 以及供 Agent 与网页交互的可脚本化 API。
/// </summary>
public partial class BrowserControl : UserControl
{
    public event Action? CloseRequested;

    public BrowserControl()
    {
        InitializeComponent();
        InitializeWebView();
    }

    private async void InitializeWebView()
    {
        try
        {
            await WebView.EnsureCoreWebView2Async();
            WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            WebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebView2 init failed: {ex.Message}");
        }
    }

    /// <summary>导航到指定 URL。</summary>
    public void Navigate(string url)
    {
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        try
        {
            WebView.CoreWebView2?.Navigate(url);
            AddressBar.Text = url;
        }
        catch
        {
            // 无效的 URL
        }
    }

    /// <summary>执行 JavaScript 并返回结果。</summary>
    public async Task<string> EvaluateJavaScript(string script)
    {
        if (WebView.CoreWebView2 == null) return "";
        return await WebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    /// <summary>获取无障碍树快照（简化版）。</summary>
    public async Task<string> GetAccessibilitySnapshot()
    {
        const string script = @"
            (function() {
                function walk(node) {
                    const result = {
                        role: node.getAttribute('role') || node.tagName.toLowerCase(),
                        name: node.getAttribute('aria-label') || node.textContent?.substring(0, 100) || '',
                        children: []
                    };
                    for (const child of node.children) {
                        result.children.push(walk(child));
                    }
                    return result;
                }
                return JSON.stringify(walk(document.body));
            })()
        ";
        return await EvaluateJavaScript(script);
    }

    /// <summary>通过 CSS 选择器点击元素。</summary>
    public async Task ClickElement(string selector)
    {
        var escapedSelector = selector.Replace("'", "\\'");
        await EvaluateJavaScript($"document.querySelector('{escapedSelector}')?.click()");
    }

    /// <summary>通过 CSS 选择器填充表单字段。</summary>
    public async Task FillElement(string selector, string value)
    {
        var escapedSelector = selector.Replace("'", "\\'");
        var escapedValue = value.Replace("'", "\\'");
        await EvaluateJavaScript($@"
            (() => {{
                const el = document.querySelector('{escapedSelector}');
                if (el) {{
                    el.value = '{escapedValue}';
                    el.dispatchEvent(new Event('input', {{ bubbles: true }}));
                    el.dispatchEvent(new Event('change', {{ bubbles: true }}));
                }}
            }})()
        ");
    }

    /// <summary>获取当前页面 URL。</summary>
    public string GetCurrentUrl()
    {
        return WebView.CoreWebView2?.Source ?? "";
    }

    // 事件处理器

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2?.CanGoBack == true)
            WebView.CoreWebView2.GoBack();
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (WebView.CoreWebView2?.CanGoForward == true)
            WebView.CoreWebView2.GoForward();
    }

    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        WebView.CoreWebView2?.Reload();
    }

    private void CloseBrowser_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke();
    }

    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Navigate(AddressBar.Text);
            e.Handled = true;
        }
    }

    private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        AddressBar.Text = WebView.CoreWebView2?.Source ?? "";
    }

    private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        AddressBar.Text = WebView.CoreWebView2?.Source ?? "";
    }
}
