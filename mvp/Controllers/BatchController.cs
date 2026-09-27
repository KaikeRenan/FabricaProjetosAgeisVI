using Microsoft.AspNetCore.Mvc;
using mvp.DTOs.Batch;
using mvp.Exceptions;
using mvp.Interfaces.IServices;

namespace mvp.Controllers
{
    [ApiController]
    [Route("api/batches")]
    public class BatchController : ControllerBase
    {
        private readonly IBatchService _batchService;

        public BatchController(IBatchService batchService)
        {
            _batchService = batchService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var batches = await _batchService.GetAllAsync();
            return Ok(batches);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById(Guid Id)
        {
            try
            {
                var batch = await _batchService.GetByIdAsync(Id);

                if (batch == null)
                    throw new BatchNotFoundException();

                return Ok(batch);
            }
            catch (BatchNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BatchCreateDTO dto)
        {
            var batch = await _batchService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = batch.Id }, batch);
        }

        [HttpPut("{Id}")]
        public async Task<IActionResult> Update(Guid Id, [FromBody] BatchUpdateDTO dto)
        {
            try
            {
                var batch = await _batchService.UpdateAsync(Id, dto);
                return Ok(batch);
            }
            catch (BatchNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            try
            {
                await _batchService.DeleteAsync(Id);
                return NoContent();
            }
            catch (BatchNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
