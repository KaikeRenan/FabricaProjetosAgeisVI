using Microsoft.AspNetCore.Mvc;
using mvp.DTOs.Medicine;
using mvp.Exceptions;
using mvp.Interfaces.IServices;

namespace mvp.Controllers
{
    [ApiController]
    [Route("api/medicines")]
    public class MedicineController : ControllerBase
    {
        private readonly IMedicineService _medicineService;

        public MedicineController(IMedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var medicines = await _medicineService.GetAllAsync();
            return Ok(medicines);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById(Guid Id)
        {
            var medicine = await _medicineService.GetByIdAsync(Id);

            if (medicine == null)
            {
                throw new MedicineNotFoundException();
            }

            return Ok(medicine);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MedicineCreateDTO dto)
        {
            var medicine = await _medicineService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = medicine.Id }, medicine);
        }

        [HttpPut("{Id}")]
        public async Task<IActionResult> Update(Guid Id, [FromBody] MedicineUpdateDTO dto)
        {
            try
            {
                var medicine = await _medicineService.UpdateAsync(Id, dto);
                return Ok(medicine);
            }
            catch (MedicineNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            try
            {
                await _medicineService.DeleteAsync(Id);
                return NoContent();
            }
            catch (MedicineNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
