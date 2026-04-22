using Microsoft.AspNetCore.Mvc;
using web_vk.Services;

namespace web_vk.Controllers;

[ApiController]
[Route("api/tts")]
public class TtsController : ControllerBase
{
    private readonly ElevenLabsService _ttsService;

    public TtsController(ElevenLabsService ttsService)
    {
        _ttsService = ttsService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] TtsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { success = false, message = "Text rỗng" });
        }

        try
        {
            var filePath = await _ttsService.GenerateAudioAsync(request.Text);

            return Ok(new
            {
                success = true,
                filePath = filePath
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message
            });
        }
    }
}

public class TtsRequest
{
    public string Text { get; set; } = string.Empty;
}