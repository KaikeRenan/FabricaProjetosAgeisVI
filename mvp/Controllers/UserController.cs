using Microsoft.AspNetCore.Mvc;
using mvp.DTOs;
using mvp.Exceptions;
using mvp.Interfaces.IServices;

namespace mvp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{Id}")]
        public async Task<IActionResult> GetById(Guid Id)
        {
            var user = await _userService.GetByIdAsync(Id);

            if (user == null)
                throw new UserNotFoundException();

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserCreateDTO dto)
        {
            var user = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPut("{Id}")]
        public async Task<IActionResult> Update(Guid Id, [FromBody] UserUpdateDTO dto)
        {
            try
            {
                var user = await _userService.UpdateAsync(Id, dto);
                return Ok(user);
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> Delete(Guid Id)
        {
            try
            {
                await _userService.DeleteAsync(Id);
                return NoContent();
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
