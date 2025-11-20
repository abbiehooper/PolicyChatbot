using PolicyChatbot.Shared;
using PolicyChatbot.Shared.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PolicyChatbot.Server.Services;

public interface IPolicyRepository
{
    /// <summary>
    /// Retrieves a list of available insurance types.
    /// </summary>
    /// <returns>A list of strings representing the names of the available insurance types.  The list will be empty if no
    /// insurance types are available.</returns>
    List<string> GetInsuranceTypes();

    /// <summary>
    /// Retrieves a list of insurers that provide coverage for the specified insurance type.
    /// </summary>
    /// <param name="insuranceType">The type of insurance to filter insurers by. For example, "Car", "Home", or "Van".</param>
    /// <returns>A list of insurer names that offer the specified type of insurance. The list will be empty if no matching
    /// insurers are found.</returns>
    List<string> GetInsurers(string insuranceType);

    /// <summary>
    /// Retrieves a list of products based on the specified insurance type and insurer.
    /// </summary>
    /// <param name="insuranceType">The type of insurance to filter products by. This value cannot be null or empty.</param>
    /// <param name="insurer">The name of the insurer to filter products by. This value cannot be null or empty.</param>
    /// <returns>A list of <see cref="ProductInfo"/> objects that match the specified criteria. Returns an empty list if no
    /// products are found.</returns>
    List<ProductInfo> GetProducts(string insuranceType, string insurer);

    /// <summary>
    /// Retrieves the policy content associated with the specified product identifier.
    /// </summary>
    /// <param name="productId">The unique identifier of the product for which the policy content is requested. Cannot be null or empty.</param>
    /// <returns>The policy content as a string if the product identifier is valid and a policy exists; otherwise, <see
    /// langword="null"/>.</returns>
    string? GetPolicyContent(string productId);
}

public class PolicyRepository : IPolicyRepository
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

    /// <inheritdoc/>
    public List<string> GetInsuranceTypes()
    {
        if (!Directory.Exists(_policyPath))
            return [];

        return Directory.GetDirectories(_policyPath)
            .Select(d => Path.GetFileName(d))
            .Where(d => !string.IsNullOrEmpty(d))
            .ToList()!;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public List<ProductInfo> GetProducts(string insuranceType, string insurer)
    {
        string? insurerPath = Path.Combine(_policyPath, insuranceType, insurer);

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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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