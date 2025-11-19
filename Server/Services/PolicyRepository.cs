using PolicyChatbot.Shared;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PolicyChatbot.Server.Services;

public class PolicyRepository
{
    private readonly string _policyPath;
    private readonly ILogger<PolicyRepository> _logger;

    public PolicyRepository(IWebHostEnvironment env, ILogger<PolicyRepository> logger)
    {
        _policyPath = Path.Combine(env.ContentRootPath, "PolicyDocuments");
        _logger = logger;

        // Ensure the base directory exists
        if (!Directory.Exists(_policyPath))
        {
            Directory.CreateDirectory(_policyPath);
            _logger.LogInformation("Created PolicyDocuments directory at: {_policyPath}", _policyPath);
        }
    }

    public List<string> GetInsuranceTypes()
    {
        if (!Directory.Exists(_policyPath))
            return new List<string>();

        return Directory.GetDirectories(_policyPath)
            .Select(d => Path.GetFileName(d))
            .Where(d => !string.IsNullOrEmpty(d))
            .ToList()!;
    }

    public List<string> GetInsurers(string insuranceType)
    {
        var typePath = Path.Combine(_policyPath, insuranceType);

        if (!Directory.Exists(typePath))
            return new List<string>();

        return Directory.GetDirectories(typePath)
            .Select(d => Path.GetFileName(d))
            .Where(d => !string.IsNullOrEmpty(d))
            .ToList()!;
    }

    public List<ProductInfo> GetProducts(string insuranceType, string insurer)
    {
        var insurerPath = Path.Combine(_policyPath, insuranceType, insurer);

        if (!Directory.Exists(insurerPath))
            return new List<ProductInfo>();

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
        try
        {
            // Parse productId: InsuranceType_Insurer_ProductName
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

            if (!File.Exists(pdfPath))
            {
                _logger.LogWarning("PDF file not found: {PdfPath}", pdfPath);
                return null;
            }

            return ExtractTextFromPdf(pdfPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading policy content for productId: {ProductId}", productId);
            return null;
        }
    }

    private string ExtractTextFromPdf(string pdfPath)
    {
        try
        {
            using var document = PdfDocument.Open(pdfPath);
            var text = new System.Text.StringBuilder();

            foreach (Page page in document.GetPages())
            {
                text.AppendLine(page.Text);
            }

            return text.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF: {PdfPath}", pdfPath);
            throw;
        }
    }
}