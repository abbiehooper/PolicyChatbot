using PolicyChatbot.Shared.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PolicyChatbot.Server.Services;

public interface IPolicyService
{
    List<string> GetInsuranceTypes();
    List<string> GetInsurers(string insuranceType);
    List<ProductInfo> GetProducts(string insuranceType, string insurer);
    string? GetPolicyContent(string productId);
    PolicyContent? GetPolicyContentWithPages(string productId);
    string? GetPdfPath(string productId);
}

public class PolicyService : IPolicyService
{
    private readonly string _policyPath;
    private readonly ILogger<PolicyService> _logger;

    public PolicyService(IWebHostEnvironment env, ILogger<PolicyService> logger)
    {
        _policyPath = Path.Combine(env.ContentRootPath, "PolicyDocuments");
        _logger = logger;

        if (!Directory.Exists(_policyPath))
        {
            Directory.CreateDirectory(_policyPath);
            _logger.LogInformation("Created PolicyDocuments directory at: {_policyPath}", _policyPath);
        }
    }

    public List<string> GetInsuranceTypes()
    {
        if (!Directory.Exists(_policyPath))
            return [];

        return Directory.GetDirectories(_policyPath)
            .Select(d => Path.GetFileName(d))
            .Where(d => !string.IsNullOrEmpty(d))
            .ToList()!;
    }

    public List<string> GetInsurers(string insuranceType)
    {
        var typePath = Path.Combine(_policyPath, insuranceType);

        if (!Directory.Exists(typePath))
            return [];

        return Directory.GetDirectories(typePath)
            .Select(d => Path.GetFileName(d))
            .Where(d => !string.IsNullOrEmpty(d))
            .ToList()!;
    }

    public List<ProductInfo> GetProducts(string insuranceType, string insurer)
    {
        string? insurerPath = Path.Combine(_policyPath, insuranceType, insurer);

        if (!Directory.Exists(insurerPath))
            return [];

        return Directory.GetFiles(insurerPath, "*.pdf")
            .Select(file =>
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var productId = $"{insuranceType}_{insurer}_{fileName}".Replace(" ", "_");

                return new ProductInfo
                {
                    Id = productId,
                    Name = fileName.Replace("_", " ")
                };
            })
            .ToList();
    }

    public string? GetPolicyContent(string productId)
    {
        var content = GetPolicyContentWithPages(productId);
        return content?.FullText;
    }

    public PolicyContent? GetPolicyContentWithPages(string productId)
    {
        try
        {
            var pdfPath = GetPdfPath(productId);
            if (pdfPath == null || !File.Exists(pdfPath))
            {
                _logger.LogWarning("PDF file not found for productId: {ProductId}", productId);
                return null;
            }

            return ExtractTextFromPdfWithPages(pdfPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading policy content for productId: {ProductId}", productId);
            return null;
        }
    }

    public string? GetPdfPath(string productId)
    {
        try
        {
            var parts = productId.Split('_', 3);
            if (parts.Length < 3)
            {
                _logger.LogWarning("Invalid productId format: {ProductId}", productId);
                return null;
            }

            var insuranceType = parts[0];
            var insurer = parts[1];
            var productName = parts[2];

            var pdfPath = Path.Combine(_policyPath, insuranceType, insurer, $"{productName}.pdf");

            return File.Exists(pdfPath) ? pdfPath : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PDF path for productId: {ProductId}", productId);
            return null;
        }
    }

    private PolicyContent ExtractTextFromPdfWithPages(string pdfPath)
    {
        try
        {
            using var document = PdfDocument.Open(pdfPath);
            var fullText = new System.Text.StringBuilder();
            var pages = new List<PolicyPage>();

            foreach (Page page in document.GetPages())
            {
                var pageText = page.Text;
                fullText.AppendLine($"[Page {page.Number}]");
                fullText.AppendLine(pageText);
                fullText.AppendLine();

                pages.Add(new PolicyPage
                {
                    PageNumber = page.Number,
                    Content = pageText
                });
            }

            return new PolicyContent
            {
                FullText = fullText.ToString(),
                Pages = pages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF: {PdfPath}", pdfPath);
            throw;
        }
    }
}
