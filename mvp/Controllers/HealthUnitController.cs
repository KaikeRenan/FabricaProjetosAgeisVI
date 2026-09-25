using Microsoft.AspNetCore.Mvc;
using mvp.DTOs;
using mvp.Exceptions;
using mvp.Interfaces.IServices;

namespace mvp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthUnitController : ControllerBase
    {
        private readonly IHealthUnittService _healthUnitService;

        public HealthUnitController(IHealthUnittService healthUnitService)
        {
            _healthUnitService = healthUnitService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var healthUnits = await _healthUnitService.GetAllAsync();
            return Ok(healthUnits);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById(Guid Id)
        {

            var healthUnit = await _healthUnitService.GetByIdAsync(Id);

            if (healthUnit == null)
            {
                throw new HealthUnitNotFoundException();
            }

            return Ok(healthUnit);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HealthUnitCreateDTO dto)
        {
            var healthUnit = await _healthUnitService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = healthUnit.Id }, healthUnit);
        }

        [HttpPut("{Id}")]
        public async Task<IActionResult> Update(Guid Id, [FromBody] HealthUnitUpdateDTO dto)
        {
            try
            {
                var healthUnit = await _healthUnitService.UpdateAsync(Id, dto);
                return Ok(healthUnit);
            }
            catch (HealthUnitNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            try
            {
                await _healthUnitService.DeleteAsync(Id);
                return NoContent();
            }
            catch (HealthUnitNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
