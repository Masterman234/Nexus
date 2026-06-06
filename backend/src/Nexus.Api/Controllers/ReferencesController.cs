using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Common.Queries.GetRelatedEntities;

namespace Nexus.Api.Controllers;

[Authorize]
public class ReferencesController(ISender sender) : ApiController(sender)
{
    [HttpGet("{entityId:guid}/related")]
    public async Task<IActionResult> GetRelated(Guid entityId)
    {
        return HandleResult(await Sender.Send(new GetRelatedEntities.Query(entityId)));
    }
}
