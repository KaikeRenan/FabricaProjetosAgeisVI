using Microsoft.AspNetCore.Mvc;
using mvp.DTOs.ExpirationAlert;
using mvp.Exceptions;
using mvp.Interfaces.IServices;

namespace mvp.Controllers
{
    [ApiController]
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
            var expirationAlerts = await _expirationAlertService.GetAllAsync();
            return Ok(expirationAlerts);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById(Guid Id)
        {
            try
            {
                var expirationAlert = await _expirationAlertService.GetByIdAsync(Id);
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
    }
}
