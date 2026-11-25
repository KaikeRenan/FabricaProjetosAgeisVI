using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mvp.DTOs.ExpirationAlert;
using mvp.Exceptions;
using mvp.Interfaces.IServices;

namespace mvp.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/expirationAlerts")]
    public class ExpirationAlertController : ControllerBase
    {
        private readonly IExpirationAlertService _expirationAlertService;

        public ExpirationAlertController(IExpirationAlertService expirationAlertService)
        {
            _expirationAlertService = expirationAlertService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var expirationAlerts = await _expirationAlertService.GetAllWithDetailsAsync();
            return Ok(expirationAlerts);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById(Guid Id)
        {
            try
            {
                var expirationAlert = await _expirationAlertService.GetByIdWithDetailsAsync(Id);
                return Ok(expirationAlert);
            }
            catch (ExpirationAlertNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ExpirationAlertCreateDTO dto)
        {
            var expirationAlert = await _expirationAlertService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = expirationAlert.Id }, expirationAlert);
        }

        [HttpPut("{Id}")]
        public async Task<IActionResult> Update(Guid Id, [FromBody] ExpirationAlertUpdateDTO dto)
        {
            try
            {
                var expirationAlert = await _expirationAlertService.UpdateAsync(Id, dto);
                return Ok(expirationAlert);
            }
            catch (ExpirationAlertNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            try
            {
                await _expirationAlertService.DeleteAsync(Id);
                return NoContent();
            }
            catch (ExpirationAlertNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("generate")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Generate([FromQuery] int daysAhead = 30, [FromQuery] bool autoResolve = true, CancellationToken ct = default)
        {
            var result = await _expirationAlertService.GenerateAsync(daysAhead, autoResolve, ct);
            return Ok(result);
        }
    }
}
