using ContentAPI.Models.Common;
using ContentAPI.Models.SavedContent.DTOs;
using ContentAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ProductAPI.Controllers
{
    /// <summary>
    /// Hanterar operationer för sparat innehåll, inklusive skapande, radering och sökning.
    /// </summary>
    [Route("api/savedcontent")]
    [ApiController]
    [EnableRateLimiting("fixed")]
    public class SavedContentController : ControllerBase
    {
        private readonly ISavedContentService _savedContentService;

        /// <summary>
        /// Initierar en ny instans av <see cref="SavedContentController"/>.
        /// </summary>
        /// <param name="savedContentService">Tjänst för Saved Content-logik.</param>
        public SavedContentController(ISavedContentService savedContentService)
        {
            _savedContentService = savedContentService;
        }

        /// <summary>
        /// Skapar nytt sparat innehåll i systemet.
        /// </summary>
        /// <param name="request">Information om det sparade innehållet som ska skapas.</param>
        /// <returns>Det nyskapade sparade innehållet.</returns>
        /// <response code="201">Det sparade innehållet skapades framgångsrikt.</response>
        /// <response code="401">Användaren är inte auktoriserad.</response>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<SavedContentResponse>> CreateSavedContent([FromBody] CreateSavedContentRequest request)
        {
            var response = await _savedContentService.CreateSavedContentAsync(request);
            return CreatedAtAction(nameof(GetSavedContentById), new { id = response.Id }, response);
        }

        /// <summary>
        /// Hämtar en sida med sparat innehåll.
        /// </summary>
        /// <param name="page">Sidnummer (standard är 1).</param>
        /// <param name="pageSize">Antal sparade innehåll per sida (standard 10, max 100).</param>
        /// <returns>En paged-lista med sparat innehåll.</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResponse<SavedContentResponse>>> GetAllSavedContent([FromQuery] SavedContentFilter filter)
        {
            // Validera sidstorlek
            var sanitizedFilter = filter with { PageSize = Math.Clamp(filter.PageSize, 1, 100) };

            var pagedResponse = await _savedContentService.GetPagedResponseAsync(sanitizedFilter);
            return Ok(pagedResponse);
        }

        /// <summary>
        /// Hämtar ett specifikt sparat innehåll baserat på dess unika ID.
        /// </summary>
        /// <param name="id">Sparat innehålls ID.</param>
        /// <returns>Det efterfrågade sparade innehållet.</returns>
        /// <response code="200">Sparat innehåll hittades.</response>
        /// <response code="404">Inget sparat innehåll hittades med det angivna ID:t.</response>
        [HttpGet("{id}", Name = "GetSavedContentById")]
        [AllowAnonymous]
        public async Task<ActionResult<SavedContentResponse>> GetSavedContentById(int id)
        {
            var response = await _savedContentService.GetSavedContentByIdAsync(id);
            return Ok(response);
        }

        /// <summary>
        /// Uppdaterar ett befintligt sparat innehåll.
        /// </summary>
        /// <param name="id">ID för det sparade innehållet som ska ändras.</param>
        /// <param name="request">Den nya informationen för det sparade innehållet.</param>
        /// <returns>Inget innehåll (204) vid lyckad uppdatering.</returns>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateSavedContent(int id, [FromBody] UpdateSavedContentRequest request)
        {
            await _savedContentService.UpdateSavedContentAsync(id, request);
            return NoContent();
        }

        /// <summary>
        /// Tar bort ett sparat innehåll från systemet. Kräver administratörsbehörighet.
        /// </summary>
        /// <param name="id">ID för det sparade innehållet som ska raderas.</param>
        /// <returns>Inget innehåll (204) vid lyckad radering.</returns>
        /// <response code="403">Användaren har inte Admin-behörighet.</response>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteSavedContent(int id)
        {
            await _savedContentService.DeleteSavedContentAsync(id);
            return NoContent();
        }
    }
}