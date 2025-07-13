using BusFinderBackend.Model;
using BusFinderBackend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Annotations;

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
        [SwaggerOperation(Summary = "Get all places.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<Place>>> GetAllPlaces()
        {
            var places = await _placeService.GetAllPlacesAsync();
            return Ok(places);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get a place by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Place>> GetPlaceById(string id)
        {
            var place = await _placeService.GetPlaceByIdAsync(id);
            if (place == null)
                return NotFound();
            return Ok(place);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Add a new place.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
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
        [SwaggerOperation(Summary = "Update a place by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> UpdatePlace(string id, [FromBody] Place place)
        {
            var existing = await _placeService.GetPlaceByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _placeService.UpdatePlaceAsync(id, place);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete a place by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> DeletePlace(string id)
        {
            var existing = await _placeService.GetPlaceByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _placeService.DeletePlaceAsync(id);
            return NoContent();
        }

        [HttpGet("search/google/{name}")]
        public async Task<ActionResult<List<Place>>> SearchUsingGoogleApi(string name)
        {
            var places = await _placeService.SearchPlacesUsingGoogleApiAsync(name);
            if (places == null || places.Count == 0)
                return NotFound(new { message = "No places found." });
            return Ok(places);
        }

        [HttpGet("search/firebase/{partialName}")]
        public async Task<ActionResult<List<Place>>> SearchPlacesByPartialName(string partialName)
        {
            var places = await _placeService.SearchPlacesByPartialNameAsync(partialName);
            if (places == null || places.Count == 0)
                return NotFound(new { message = "No places found matching the search criteria." });
            return Ok(places);
        }
    }
}
