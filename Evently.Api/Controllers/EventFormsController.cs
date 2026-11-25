using System.Security.Claims;
using Evently.Api.Data;
using Evently.Api.Data.Mongo;
using Evently.Api.Models;
using Evently.Api.Models.Mongo;
using Evently.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Evently.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventFormsController : ControllerBase
    {
        private readonly EventlyMongoContext _mongoContext;

        public EventFormsController(EventlyMongoContext mongoContext)
        {
            _mongoContext = mongoContext;
        }

        [Authorize(Roles = "Organizer,Admin")]
        [HttpPost("{eventId}")]
        public async Task<IActionResult> CreateForm(Guid eventId, [FromBody] EventFormDto dto)
        {
            var form = new EventForm
            {
                EventId = eventId,
                Fields = dto.Fields.Select(f => new FormField
                {
                    Label = f.Label,
                    Name = f.Name,
                    Type = f.Type,
                    Required = f.Required,
                    Options = f.Options
                }).ToList()
            };

            await _mongoContext.EventForms.DeleteManyAsync(f => f.EventId == eventId);
            await _mongoContext.EventForms.InsertOneAsync(form);
            return Created("", form);
        }

        [HttpGet("{eventId}")]
        public async Task<IActionResult> GetForm(Guid eventId)
        {
            var form = await _mongoContext.EventForms
                .Find(f => f.EventId == eventId)
                .FirstOrDefaultAsync();

            if (form == null) return NotFound();
            return Ok(form);
        }
    }

    public class EventFormDto
    {
        public List<FormFieldDto> Fields { get; set; } = new();
    }

    public class FormFieldDto
    {
        public string Label { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; }
        public List<string>? Options { get; set; }
    }
}
