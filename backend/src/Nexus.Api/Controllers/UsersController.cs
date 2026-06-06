using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Auth.Queries.GetUsers;

namespace Nexus.Api.Controllers;

[Authorize]
public class UsersController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        return HandleResult(await Sender.Send(new GetUsers.Query()));
    }
}
