using System.Collections.Generic;
using Foxpict.Service.Infra;

namespace Foxpict.Service.Web {
  /// <summary>
  /// ビルド用パラメータ
  /// </summary>
  public class BuildAssemblyParameter : IBuildAssemblyParameter {
    private readonly Dictionary<string, string> _Params = new Dictionary<string, string> ();

    /// <summary>
    /// 任意のパラメータ
    /// </summary>
    public Dictionary<string, string> Params => _Params;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public BuildAssemblyParameter () {
      BuildParams ();
    }

    /// <summary>
    /// コンストラクタ
    /// /// </summary>
    /// <param name="appSettings"></param>
    public BuildAssemblyParameter (AppSettings appSettings) {
      this.Params.Add ("ApplicationDirectoryPath", appSettings.ApplicationDirectoryBasePath);
      if (!string.IsNullOrEmpty (appSettings.InitializeSqlAppDb)) {
        this.Params.Add ("InitializeSqlAppDb", appSettings.InitializeSqlAppDb);
      }
      if (appSettings.AbsoluteApplicationDirectoryBase) {
        this.Params.Add ("AbsoluteApplicationDirectoryBase", "true");
      } else {
        this.Params.Add ("AbsoluteApplicationDirectoryBase", "false");
      }
    }

    private void BuildParams () {
      if (!_Params.ContainsKey ("ApplicationDirectoryPath")) {
        this.Params.Add ("ApplicationDirectoryPath", @"Foxpict.Srv");
      }
    }
  }
}
