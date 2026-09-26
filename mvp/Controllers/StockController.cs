using Microsoft.AspNetCore.Mvc;
using mvp.DTOs;
using mvp.Exceptions;
using mvp.Interfaces.IServices;

namespace mvp.Controllers
{
    [ApiController]
    [Route("api/stocks")]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;

        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var stocks = await _stockService.GetAllAsync();
            return Ok(stocks);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById(Guid Id)
        {
            var stock = await _stockService.GetByIdAsync(Id);
            if (stock == null) throw new StockNotFoundException();
            return Ok(stock);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockCreateDTO dto)
        {
            var stock = await _stockService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = stock.Id }, stock);
        }

        [HttpPut("{Id}")]
        public async Task<IActionResult> Update(Guid Id, [FromBody] StockUpdateDTO dto)
        {
            try
            {
                var stock = await _stockService.UpdateAsync(Id, dto);
                return Ok(stock);
            }
            catch (StockNotFoundException ex) { return NotFound(ex.Message); }
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            try
            {
                await _stockService.DeleteAsync(Id);
                return NoContent();
            }
            catch (StockNotFoundException ex) { return NotFound(ex.Message); }
        }
    }
}
