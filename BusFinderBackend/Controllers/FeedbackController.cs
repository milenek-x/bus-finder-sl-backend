using Microsoft.AspNetCore.Mvc;
using BusFinderBackend.Services;
using BusFinderBackend.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Annotations;
using BusFinderBackend.DTOs.Feedback;

namespace BusFinderBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly FeedbackService _feedbackService;

        public FeedbackController(FeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all feedbacks.")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetAllFeedbacks()
        {
            var feedbacks = await _feedbackService.GetAllFeedbacksAsync();
            return Ok(feedbacks);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get feedback by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetFeedbackById(string id)
        {
            var feedback = await _feedbackService.GetFeedbackByIdAsync(id);
            if (feedback == null)
                return NotFound();
            return Ok(feedback);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Add new feedback.")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddFeedback([FromBody] FeedbackCreateDto feedbackDto)
        {
            var result = await _feedbackService.AddFeedbackAsync(feedbackDto);
            if (!result.Success)
                return BadRequest(new { errorCode = result.ErrorCode, errorMessage = result.ErrorMessage });
            return CreatedAtAction(nameof(GetFeedbackById), new { id = result.FeedbackId }, result.Feedback);
        }

        [HttpPut("{id}/reply")]
        [SwaggerOperation(Summary = "Reply to feedback by ID.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ReplyFeedback(string id, [FromBody] FeedbackReplyDto replyDto)
        {
            var result = await _feedbackService.ReplyFeedbackAsync(id, replyDto);
            if (!result.Success)
                return BadRequest(new { errorCode = result.ErrorCode, errorMessage = result.ErrorMessage });
            return Ok();
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete feedback by ID.")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteFeedback(string id)
        {
            await _feedbackService.DeleteFeedbackAsync(id);
            return NoContent();
        }
    }
} 