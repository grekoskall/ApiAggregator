using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiAggregation.Models;
using ApiAggregation.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AggregationController : ControllerBase
    {
        private readonly IApiAggregationService _aggregationService;

        public AggregationController(IApiAggregationService aggregationService)
        {
            _aggregationService = aggregationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AggregatedData>>> GetAll()
        {
            var data = await _aggregationService.GetAggregatedDataAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AggregatedData>> GetById(int id)
        {
            var data = await _aggregationService.GetAggregatedDataByIdAsync(id);
            if (data == null)
            {
                return NotFound();
            }
            return Ok(data);
        }

        [HttpPost]
        public async Task<ActionResult<AggregatedData>> Create(AggregatedData data)
        {
            var result = await _aggregationService.AddAggregatedDataAsync(data);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPost("fetch")]
        public async Task<ActionResult<AggregatedData>> FetchFromExternalApi([FromBody] ExternalApiRequest request)
        {
            try
            {
                var result = await _aggregationService.FetchFromExternalApiAsync(request.ApiUrl);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class ExternalApiRequest
    {
        public string ApiUrl { get; set; }
    }
}