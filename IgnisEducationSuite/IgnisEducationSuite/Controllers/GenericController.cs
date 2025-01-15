using EduSphereDomain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IgnisEducationSuite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenericController<T> : ControllerBase where T : class
    {
        private readonly IGenericRepository<T> _repository;

       public GenericController(IGenericRepository<T> repository)
        {
          _repository = repository;
        }
        // GET: api/[controller]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var entities = await _repository.GetAllAsync();
            return Ok(entities);
        }

        // GET: api/[controller]/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }

        // POST: api/[controller]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] T entity)
        {
            if (entity == null)
            {
                return BadRequest();
            }

            await _repository.AddAsync(entity);
            return CreatedAtAction(nameof(Get), new { id = entity.GetHashCode() }, entity);  // Assuming the entity has an ID
        }

        // PUT: api/[controller]/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] T entity)
        {
            if (entity == null)
            {
                return BadRequest();
            }

            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                return NotFound();
            }

            await _repository.UpdateAsync(entity);
            return NoContent();
        }

        // DELETE: api/[controller]/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }
}
