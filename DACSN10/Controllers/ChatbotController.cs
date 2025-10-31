using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DACSN10.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(IHttpClientFactory httpClientFactory, ILogger<ChatbotController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public class AskRequest
        {
            public string Question { get; set; } = "";
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] AskRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Question))
                return BadRequest(new { error = "Question is empty" });

            try
            {
                // Gọi service Python qua HttpClient "AiApi"
                var client = _httpClientFactory.CreateClient("AiApi");

                var jsonPayload = JsonSerializer.Serialize(new { question = req.Question });
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Gửi POST /api/ask tới Python
                var resp = await client.PostAsync("/api/ask", content);

                var respBody = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("AI API error: {Status} {Body}", resp.StatusCode, respBody);
                    return StatusCode(500, new { error = "AI service error", detail = respBody });
                }

                // Trả thẳng JSON Python trả về cho frontend
                return Content(respBody, "application/json", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AI API");
                return StatusCode(500, new { error = "Exception when calling AI API" });
            }
        }
    }
}
