using Foxpict.Service.Infra.Model;

namespace Foxpict.Service.Web {
  /// <summary>
  /// アプリケーション設定モデル
  /// </summary>
  public class AppSettings : IAppSettings {
    /// <summary>
    ///   /// アプリケーションが使用するフォルダの基本パスを設定、または取得します。
    /// </summary>
    /// <returns></returns>
    public string ApplicationDirectoryBasePath { get; set; }

    /// <summary>
    /// "ApplicationDirectoryBasePath"が示すディレクトリが、絶対パスを示しているか
    /// </summary>
    /// <returns></returns>
    public bool AbsoluteApplicationDirectoryBase { get; set; }

    /// <summary>
    /// データベース初期化で使用するSQLファイルのリソースパス
    /// </summary>
    /// <returns></returns>
    public string InitializeSqlAppDb { get; set; }

    /// <summary>
    /// デフォルトワークスペース
    /// </summary>
    /// <returns></returns>
    public DefaultWorkspace Workspace { get; set; }

    /// <summary>
    /// 外部のデータベースを使用する場合は、接続先URLを設定します
    /// </summary>
    /// <value></value>
    public string ENV_HEROKU_DATABASE_URL { get; set; }

    /// <summary>
    /// デフォリトワークスペース情報
    /// </summary>
    public class DefaultWorkspace {
      /// <summary>
      /// ワークスペース名称
      /// </summary>
      /// <value></value>
      public string Name { get; set; }

      /// <summary>
      /// ワークスペースのディレクトリパス
      /// </summary>
      /// <value></value>
      public bool RelativeApplicationDirectoryBasePath { get; set; }

      /// <summary>
      /// VFS用の格納パス
      /// </summary>
      /// <value></value>
      public string VirtualPath { get; set; }

      /// <summary>
      /// VFSの格納パス
      /// </summary>
      /// <value></value>
      public string PhysicalPath { get; set; }
    }
  }
}
