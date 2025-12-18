using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet.Dtos;
using dotnet.Service.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dotnet.Controllers.admin
{
  [Route("admin/[controller]")]
  [ApiController]
  public class AddressController : ControllerBase
  {
    private readonly IAddressService _service;
    public AddressController(IAddressService service)
    {
      _service = service;
    }
    [HttpPost]
    [Authorize(Roles = "0")]
    public IActionResult getAddressByIdUser([FromBody] BaseRequest dto)
    {
      var list = _service.getAddressByIdUser(dto.id);
      return Ok(new
      {
        status = 200,
        data = list,
        message = "Success"
      });
    }
  }
}