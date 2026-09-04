using Microsoft.AspNetCore.Mvc;
using mvp.DTOs;
using mvp.Exceptions;
using mvp.Interfaces.IServices;

namespace mvp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthPostController : ControllerBase
    {
        private readonly IHealthPostService _healthPostService;

        public HealthPostController(IHealthPostService healthPostService)
        {
            _healthPostService = healthPostService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var healthPosts = await _healthPostService.GetAllAsync();
            return Ok(healthPosts);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById(Guid Id)
        {

            var healthPost = await _healthPostService.GetByIdAsync(Id);

            if (healthPost == null)
            {
                throw new HealthPostNotFoundException();
            }

            return Ok(healthPost);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HealthPostCreateDTO dto)
        {
            var healthPost = await _healthPostService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = healthPost.Id }, healthPost);
        }

        [HttpPut("{Id}")]
        public async Task<IActionResult> Update(Guid Id, [FromBody] HealthPostUpdateDTO dto)
        {
            try
            {
                var healthPost = await _healthPostService.UpdateAsync(Id, dto);
                return Ok(healthPost);
            }
            catch (HealthPostNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            try
            {
                await _healthPostService.DeleteAsync(Id);
                return NoContent();
            }
            catch (HealthPostNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
