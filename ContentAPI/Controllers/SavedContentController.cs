using ContentAPI.Models.Common;
using ContentAPI.Models.SavedContent.DTOs;
using ContentAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ContentAPI.Controllers
{
    [Route("api/savedcontent")]
    [ApiController]
    [EnableRateLimiting("fixed")]
    public class SavedContentController : ControllerBase
    {
        private readonly ISavedContentService _savedContentService;

        public SavedContentController(ISavedContentService savedContentService)
        {
            _savedContentService = savedContentService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<SavedContentResponse>> CreateSavedContent([FromBody] CreateSavedContentRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var response = await _savedContentService.CreateSavedContentAsync(request);
            return CreatedAtAction(nameof(GetSavedContentById), new { id = response.Id }, response);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResponse<SavedContentResponse>>> GetAllSavedContent([FromQuery] SavedContentFilter filter)
        {
            var sanitizedFilter = filter with { PageSize = Math.Clamp(filter.PageSize, 1, 100) };
            var pagedResponse = await _savedContentService.GetPagedResponseAsync(sanitizedFilter);
            return Ok(pagedResponse);
        }

        [HttpGet("{id}", Name = "GetSavedContentById")]
        [AllowAnonymous]
        public async Task<ActionResult<SavedContentResponse>> GetSavedContentById(int id)
        {
            var response = await _savedContentService.GetSavedContentByIdAsync(id);
            return Ok(response);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateSavedContent(int id, [FromBody] UpdateSavedContentRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _savedContentService.UpdateSavedContentAsync(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSavedContent(int id)
        {
            await _savedContentService.DeleteSavedContentAsync(id);
            return NoContent();
        }
    }
}