using Microsoft.AspNetCore.Mvc;
using PolicyChatbot.Server.Services;
using PolicyChatbot.Shared.Models;
using System.Text.Json;

namespace PolicyChatbot.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PolicyController(IPolicyService policyService) : ControllerBase
{
    private readonly IPolicyService _policyService = policyService;

    [HttpGet("insurance-types")]
    public ActionResult<List<string>> GetInsuranceTypes()
    {
        return Ok(_policyService.GetInsuranceTypes());
    }

    [HttpGet("insurers")]
    public ActionResult<List<string>> GetInsurers([FromQuery] string insuranceType)
    {
        return Ok(_policyService.GetInsurers(insuranceType));
    }

    [HttpGet("products")]
    public ActionResult<List<ProductInfo>> GetProducts([FromQuery] string insuranceType, [FromQuery] string insurer)
    {
        return Ok(_policyService.GetProducts(insuranceType, insurer));
    }
}