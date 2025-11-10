using dotnet.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet.Controllers.admin
{
    [Route("admin/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
    private readonly IUserService _service;
    public UserController(IUserService service)
    {
      _service = service;
    }
    [HttpGet]
    [Authorize(Roles = "0")]
    public IActionResult getUsersAdmin(){
      var users = _service.getUsers();
      return Ok(new
      {
        status = 200,
        data = users,
        message = "Success"
      });
    }
  }
}