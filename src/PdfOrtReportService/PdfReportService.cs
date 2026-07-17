using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace PdfOrtReportService;

public sealed class PdfReportService
{
   
    public async Task<byte[]> GeneratePdfAsync(string html, CancellationToken cancellationToken = default)
    {
        var browserFetcher = Puppeteer.CreateBrowserFetcher(new BrowserFetcherOptions());
        var installedBrowser = await browserFetcher.DownloadAsync();
        var tempHtmlPath = Path.Combine(Path.GetTempPath(), $"pdf-render-{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(tempHtmlPath, html, cancellationToken);
        
        try
        {
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = installedBrowser.GetExecutablePath(),
                Args =
                [
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--allow-file-access-from-files",
                    "--enable-local-file-accesses",
                    "--disable-web-security"
                ]
            });

            await using var page = await browser.NewPageAsync();
            await page.GoToAsync(new Uri(tempHtmlPath).AbsoluteUri, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle0]
            });

            await page.EvaluateExpressionAsync("document.fonts ? document.fonts.ready : Promise.resolve()"); // Espera a que las fuentes estén listas
            await page.EvaluateExpressionAsync("Promise.all(Array.from(document.images).map(img => img.complete ? Promise.resolve() : new Promise(resolve => { img.onload = resolve; img.onerror = resolve; })))"); // Espera a que las imágenes estén cargadas
            var failedImages = await page.EvaluateFunctionAsync<int>("() => Array.from(document.images).filter(img => !img.complete || img.naturalWidth === 0).length"); // Verifica si hay imágenes que no se cargaron correctamente
            if (failedImages > 0)
            {
                throw new InvalidOperationException($"No se pudieron cargar {failedImages} imagen(es) del HTML antes de generar el PDF.");
            }

            return await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                DisplayHeaderFooter = true,
                HeaderTemplate = "<div></div>",
                FooterTemplate = """
                                 <div style="font-size:8px; width:100%; text-align:center; padding:0 10px;">
                                     Página <span class="pageNumber"></span> de <span class="totalPages"></span>
                                 </div>
                                 """,
                MarginOptions = new MarginOptions
                {
                    Top = "20px",
                    Bottom = "45px",
                    Left = "20px",
                    Right = "20px"
                }
            });
        }
        finally
        {
            if (File.Exists(tempHtmlPath))
            {
                File.Delete(tempHtmlPath);
            }
        }
    }

    public byte[] GeneratePdf(string html, CancellationToken cancellationToken = default)
    {
        
        return GeneratePdfAsync(html, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }
    
}