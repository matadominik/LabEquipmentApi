using FluentValidation;
using LabEquipmentApi.Contracts.Equipment;
using LabEquipmentApi.Data;
using LabEquipmentApi.Domain;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LabEquipmentApi.Controllers
{
    [Route("api/equipment")]
    [ApiController]
    public class EquipmentController : ControllerBase
    {
        private readonly LabDbContext _context;
        private readonly IValidator<CreateEquipmentRequest> _createValidator;

        public EquipmentController(LabDbContext context, IValidator<CreateEquipmentRequest> createValidator)
        {
            _context = context;
            _createValidator = createValidator;
        }

        // GET: api/Equipment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Equipment>>> GetEquipmentItems()
        {
            return await _context.EquipmentItems.ToListAsync();
        }

        // GET: api/Equipment/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Equipment>> GetEquipment(int id)
        {
            var equipment = await _context.EquipmentItems.FindAsync(id);

            if (equipment == null)
            {
                return NotFound();
            }

            return equipment;
        }

        [HttpGet("available")]
        public async Task<ActionResult<List<EquipmentListItemResponse>>> GetAvailable()
        {
            return await _context.EquipmentItems
                .Where(e => e.IsAvailable)
                .OrderBy(e => e.Name)
                .ProjectToType<EquipmentListItemResponse>()
                .ToListAsync();
        }

        [HttpGet("room-summary")]
        public async Task<ActionResult<List<RoomSummaryResponse>>> GetRoomSummary()
        {
            return await _context.EquipmentItems
                .GroupBy(e => e.Room)
                .Select(g => new RoomSummaryResponse
                {
                    Room = g.Key,
                    TotalCount = g.Count(),
                    AvailableCount = g.Count(e => e.IsAvailable)
                })
                .OrderBy(r => r.Room)
                .ToListAsync();
        }

        // PUT: api/Equipment/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEquipment(int id, Equipment equipment)
        {
            if (id != equipment.Id)
            {
                return BadRequest();
            }

            _context.Entry(equipment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EquipmentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Equipment
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Equipment>> PostEquipment(CreateEquipmentRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

                return ValidationProblem(ModelState);
            }

            var equipment = new Equipment
            {
                Name = request.Name,
                Room = request.Room,
                IsAvailable = true
            };

            _context.EquipmentItems.Add(equipment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEquipment), new { id = equipment.Id }, equipment);
        }

        // DELETE: api/Equipment/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipment(int id)
        {
            var equipment = await _context.EquipmentItems.FindAsync(id);
            if (equipment == null)
            {
                return NotFound();
            }

            _context.EquipmentItems.Remove(equipment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EquipmentExists(int id)
        {
            return _context.EquipmentItems.Any(e => e.Id == id);
        }
    }
}
