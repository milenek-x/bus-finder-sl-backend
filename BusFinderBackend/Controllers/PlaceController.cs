using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusFinderBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaceController : ControllerBase
    {
        private readonly PlaceService _placeService;

        public PlaceController(PlaceService placeService)
        {
            _placeService = placeService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Place>>> GetAllPlaces()
        {
            var places = await _placeService.GetAllPlacesAsync();
            return Ok(places);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Place>> GetPlaceById(string id)
        {
            var place = await _placeService.GetPlaceByIdAsync(id);
            if (place == null)
                return NotFound();
            return Ok(place);
        }

        [HttpPost]
        public async Task<IActionResult> AddPlace([FromBody] Place place)
        {
            var result = await _placeService.AddPlaceAsync(place);
            if (!result.Success)
            {
                return BadRequest(new
                {
                    error = result.ErrorCode,
                    message = result.ErrorMessage
                });
            }
            return CreatedAtAction(nameof(GetPlaceById), new { id = place.PlaceId }, place);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePlace(string id, [FromBody] Place place)
        {
            var existing = await _placeService.GetPlaceByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _placeService.UpdatePlaceAsync(id, place);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePlace(string id)
        {
            var existing = await _placeService.GetPlaceByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _placeService.DeletePlaceAsync(id);
            return NoContent();
        }
    }
}
