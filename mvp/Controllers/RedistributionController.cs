using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mvp.DTOs.Redistribution;
using mvp.Enums;
using mvp.Interfaces.IServices;

namespace mvp.Controllers
{
    /// <summary>
    /// Remanejamento de medicamentos guiado por previsão de demanda. Exige JWT.
    /// Erros de domínio (404/409/503) são traduzidos pelo DomainExceptionHandler.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/redistribution")]
    public class RedistributionController : ControllerBase
    {
        private readonly IRedistributionService _redistributionService;

        public RedistributionController(IRedistributionService redistributionService)
        {
            _redistributionService = redistributionService;
        }

        private string CurrentUser => User.Identity?.Name ?? "unknown";

        /// <summary>Gera novas sugestões (chama o serviço de ML) e substitui as pendentes anteriores.</summary>
        [HttpPost("runs")]
        public async Task<IActionResult> CreateRun([FromBody] RedistributionRunRequestDTO dto, CancellationToken cancellationToken)
        {
            var run = await _redistributionService.GenerateRunAsync(dto, CurrentUser, cancellationToken);
            return CreatedAtAction(nameof(GetRun), new { id = run.Id }, run);
        }

        [HttpGet("runs/{id:guid}")]
        public async Task<IActionResult> GetRun(Guid id)
        {
            var run = await _redistributionService.GetRunAsync(id);
            return Ok(run);
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions(
            [FromQuery] Guid? runId,
            [FromQuery] RedistributionSuggestionStatus? status,
            [FromQuery] int take = 100)
        {
            var suggestions = await _redistributionService.GetSuggestionsAsync(runId, status, take);
            return Ok(suggestions);
        }

        /// <summary>Aceita e EXECUTA o remanejamento (baixa na origem, entrada no destino, movimentações).</summary>
        [HttpPost("suggestions/{id:guid}/accept")]
        public async Task<IActionResult> Accept(Guid id)
        {
            var suggestion = await _redistributionService.AcceptAsync(id, CurrentUser);
            return Ok(suggestion);
        }

        /// <summary>Recusa com motivo obrigatório (o motivo é usado para avaliar e melhorar o modelo).</summary>
        [HttpPost("suggestions/{id:guid}/reject")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectSuggestionDTO dto)
        {
            var suggestion = await _redistributionService.RejectAsync(id, dto, CurrentUser);
            return Ok(suggestion);
        }

        [HttpGet("model")]
        public async Task<IActionResult> GetModelInfo(CancellationToken cancellationToken)
        {
            var info = await _redistributionService.GetModelInfoAsync(cancellationToken);
            return Ok(info);
        }
    }
}
