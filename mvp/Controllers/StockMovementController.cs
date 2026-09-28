using Microsoft.AspNetCore.Mvc;
using mvp.DTOs.StockMovement;
using mvp.Exceptions;
using mvp.Interfaces.IServices;
using mvp.Services;

namespace mvp.Controllers
{
    [ApiController]
    [Route("api/stockMovements")]
    public class StockMovementController : ControllerBase
    {
        private readonly IStockMovementService _stockMovementService;

        public StockMovementController(IStockMovementService stockMovementService)
        {
            _stockMovementService = stockMovementService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var stockMovements = await _stockMovementService.GetAllAsync();
            return Ok(stockMovements);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById(Guid Id)
        {
            try
            {
                var stockMovement = await _stockMovementService.GetByIdAsync(Id);
                return Ok(stockMovement);
            }
            catch (StockMovementNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockMovementCreateDTO dto)
        {
            var stockMovement = await _stockMovementService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = stockMovement.Id }, stockMovement);
        }

        [HttpPut("{Id}")]
        public async Task<IActionResult> Update(Guid Id, [FromBody] StockMovementUpdateDTO dto)
        {
            try
            {
                var stockMovement = await _stockMovementService.UpdateAsync(Id, dto);
                return Ok(stockMovement);
            }
            catch (StockMovementNotFoundException ex) 
            { 
                return NotFound(ex.Message); 
            }
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            try
            {
                await _stockMovementService.DeleteAsync(Id);
                return NoContent();
            }
            catch (StockMovementNotFoundException ex) 
            { 
                return NotFound(ex.Message); 
            }
        }
    }
}
