using Foxpict.Common.Core;
using Foxpict.Service.Infra.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Foxpict.Service.Web.Controllers {
  /// <summary>
  ///
  /// </summary>
  [Route ("aapi/[controller]")]
  public class DevelopController : Controller {
    private readonly IWorkspaceRepository workspaceRepository;
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="workspaceRepository"></param>
    public DevelopController (IWorkspaceRepository workspaceRepository) {
      this.workspaceRepository = workspaceRepository;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    [HttpGet ("version")]
    public ResponseAapi<string> Get_Version () {
      var response = new ResponseAapi<string> ();
      response.Value = "1.0.0";
      return response;
    }

    /// <summary>
    ///
    /// /// </summary>
    /// <returns></returns>
    [HttpGet ("register_workspace")]
    public ResponseAapi<string> Get_RegisterWorkspace () {
      var workspace = workspaceRepository.New ();
      workspace.Name = "Private";
      workspace.PhysicalPath = "/home/atachi/PixstockSample";
      workspaceRepository.Save ();

      var response = new ResponseAapi<string> ();
      response.Value = "1.0.0";
      return response;
    }
  }
}
