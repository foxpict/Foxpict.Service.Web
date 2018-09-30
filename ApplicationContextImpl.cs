using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Foxpict.Service.Core;
using Foxpict.Service.Core.Vfs;
using Foxpict.Service.Extention.Sdk;
using Foxpict.Service.Gateway;
using Foxpict.Service.Gateway.Repository;
using Foxpict.Service.Infra;
using Foxpict.Service.Infra.Core;
using Foxpict.Service.Infra.Repository;
using Foxpict.Service.Model;
using Foxpict.Service.Web.Builder;
using Hyperion.Pf.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NLog;
using SimpleInjector;
using SimpleInjector.Lifestyles;

[assembly : InternalsVisibleTo ("Foxpict.Service.Web.Tests")]
namespace Foxpict.Service.Web {
  /// <summary>
  /// アプリケーションコンテキスト
  /// </summary>
  public class ApplicationContextImpl : IApplicationContext {
    private readonly Logger mLogger;

    /// <summary>
    /// リリースバリエーション別パラメータ
    /// </summary>
    public IBuildAssemblyParameter _AssemblyParameter;

    private string _ApplicationDirectoryPath;

    //private bool _alreadyDisposed = false;

    private SimpleInjector.Container mContainer;

    /// <summary>
    /// アプリケーションディレクトリ
    /// </summary>
    public string ApplicationDirectoryPath => _ApplicationDirectoryPath;

    /// <summary>
    /// アプリケーションアセンブリのバージョン
    /// </summary>
    /// <value></value>
    public System.Diagnostics.FileVersionInfo ApplicationFileVersionInfo {
      get;
      private set;
    }

    /// <summary>
    /// データベースファイルを格納するディレクトリパス
    /// </summary>
    /// <returns></returns>
    public string DatabaseDirectoryPath => Path.Combine (ApplicationDirectoryPath, @"db");

    /// <summary>
    /// 拡張機能ファイルを格納するディレ浮くトリパス
    /// </summary>
    /// <returns></returns>
    public string ExtentionDirectoryPath => Path.Combine (ApplicationDirectoryPath, @"extention");

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="parameter"></param>
    public ApplicationContextImpl (IBuildAssemblyParameter parameter) {
      mLogger = LogManager.GetCurrentClassLogger ();

      _ApplicationDirectoryPath = "";

      _AssemblyParameter = parameter;

      if (parameter.Params["AbsoluteApplicationDirectoryBase"] == "true") {
        _ApplicationDirectoryPath = _AssemblyParameter.Params["ApplicationDirectoryPath"];
      } else {
        _ApplicationDirectoryPath = Path.Combine (
          System.Environment.GetFolderPath (Environment.SpecialFolder.UserProfile),
          _AssemblyParameter.Params["ApplicationDirectoryPath"]);
      }

      this.ApplicationFileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo (System.Reflection.Assembly.GetExecutingAssembly ().Location);
      mLogger.Info ("_ApplicationDirectoryPath = " + _ApplicationDirectoryPath);
    }

    /// <summary>
    /// DIコンテナへ生成インスタンスの登録を行います
    /// </summary>
    /// <param name="container"></param>
    public void SetDiContainer (SimpleInjector.Container container) {
      this.mContainer = container;

      container.RegisterInstance<IApplicationContext> (this);
      container.Register<ICategoryRepository, CategoryRepository> ();
      container.Register<IContentRepository, ContentRepository> ();
      container.Register<IEventLogRepository, EventLogRepository> ();
      container.Register<IFileMappingInfoRepository, FileMappingInfoRepository> ();
      container.Register<ILabelRepository, LabelRepository> ();
      container.Register<IWorkspaceRepository, WorkspaceRepository> ();
      container.Register<IAppAppMetaInfoRepository, AppAppMetaInfoRepository> ();
      container.Register<IThumbnailAppMetaInfoRepository, ThumbnailAppMetaInfoRepository> ();
      container.Register<IThumbnailRepository, ThumbnailRepository> ();
      container.Register<ApiResponseBuilder> ();
      container.Register<IAppDbContext, AppDbContext> (Lifestyle.Scoped);
      container.Register<IThumbnailDbContext, ThumbnailDbContext> (Lifestyle.Scoped);
      container.Register<IThumbnailBuilder, ThumbnailBuilder> ();
      container.Register<IFileUpdateRunner, FileUpdateRunner> ();

      // メッセージング機能
      var messagingManager = new MessagingManager (container);
      container.RegisterInstance<MessagingManager> (messagingManager);
      container.RegisterInstance<IMessagingManager> (messagingManager);

      // 拡張機能
      var extentionManager = new ExtentionManager (container);
      //extentionManager.AddPlugin (typeof (FullBuildExtention)); // 開発中は常に拡張機能を読み込む
      //extentionManager.AddPlugin (typeof (WebScribeExtention)); // 開発中は常に拡張機能を読み込む
      container.RegisterInstance<ExtentionManager> (extentionManager);
      extentionManager.InitializePlugin (ExtentionDirectoryPath);
      extentionManager.CompletePlugin ();

      // VFS機能
      var vspFileUpdateWatchManager = new VspFileUpdateWatchManager (container);
      container.RegisterInstance<VspFileUpdateWatchManager> (vspFileUpdateWatchManager);
    }

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize () {
      mLogger.Trace ("アプリケーションの初期化を開始します");
      try {
        CreateSettingSQLite ();

        InitializeDirectory ();
        InitializeAppDatabase ();
        InitializeThumbnailDatabase ();
      } catch (Exception expr) {
        mLogger.Error (expr, "初期化に失敗しました.", expr.Message);
        throw new ApplicationException ();
      }
      mLogger.Trace ("アプリケーションの初期化を終了します");
    }

    /// <summary>
    /// SQLiteを使用するための設定を読み込みます
    /// </summary>
    void CreateSettingSQLite () {
      SqliteConnectionStringBuilder builder_AppDb = new SqliteConnectionStringBuilder ();
      builder_AppDb.DataSource = Path.Combine (DatabaseDirectoryPath, "foxpict.db");

      // TODO: BuilderをDatabaseContextに設定する
    }

    /// <summary>
    /// 必要なディレクトリを作成する初期化処理
    /// </summary>
    private void InitializeDirectory () {
      mLogger.Trace ("IN");
      // アプリケーションが使用する各種ディレクトリの作成
      System.IO.Directory.CreateDirectory (ApplicationDirectoryPath);
      System.IO.Directory.CreateDirectory (DatabaseDirectoryPath);
      System.IO.Directory.CreateDirectory (ExtentionDirectoryPath);
      mLogger.Trace ("END");
    }

    /// <summary>
    /// データベースに関する初期化処理
    /// </summary>
    private void InitializeAppDatabase () {
      mLogger.Trace ("IN");
      AppMetaInfo apMetadata = null;
      bool isMigrate = false;

      const string appdb_structure_version_key = "APPDB_VER";

      var @dbc = (AppDbContext) mContainer.GetInstance<IAppDbContext> (); // DIコンテナがリソースの開放を行う
      bool isInitializeDatabase = false;
      var @repo = new AppAppMetaInfoRepository (@dbc);
      try {
        apMetadata = @repo.FindBy (p => p.Key == appdb_structure_version_key).FirstOrDefault ();
        if (apMetadata == null) isInitializeDatabase = true;
      } catch (Exception) {
        mLogger.Info ($"データベースに、{@appdb_structure_version_key}が見つからないため、初期化処理として継続します。");
        isInitializeDatabase = true;
      }

      if (isInitializeDatabase) {
        // データベースにテーブルなどの構造を初期化する
        string sqltext = "";
        System.Reflection.Assembly assm = System.Reflection.Assembly.GetExecutingAssembly ();
        string filePath = string.Format ("Foxpict.Service.Web.Assets.Sql.{0}.Initialize_sql.txt", "App");

        mLogger.Info ($"初期化SQLを'{@filePath}'から読み込みます");
        using (var stream = assm.GetManifestResourceStream (filePath)) {
          if (stream == null) {
            throw new ApplicationException ("初期化SQLファイルの読み込みに失敗しました。");
          }
          using (StreamReader reader = new StreamReader (stream)) {
            sqltext = reader.ReadToEnd ();
          }
        }

        mLogger.Info ("SQLファイル({FilePath})から、CREATEを読み込みます", filePath);
        @dbc.Database.ExecuteSqlCommand (sqltext);
        @dbc.SaveChanges ();

        // 初期データを格納する
        var repo_Category = new CategoryRepository (@dbc);
        repo_Category.Add (new Category {
          Id = 1L,
            Name = "ROOT"
        });

        apMetadata = @repo.FindBy (p => p.Key == appdb_structure_version_key).FirstOrDefault ();
      }

      if (apMetadata == null) {
        apMetadata = new AppMetaInfo { Key = appdb_structure_version_key, Value = "1.0.0" };
        @repo.Add (apMetadata);
        @repo.Save ();

        // AppMetadataが存在しない場合のみ、初期値の書き込みを行う。
        // 初期値は、キッティングから取得したリソースパスが示すSQLファイルからデータベースの初期値を読み込む。
        if (this._AssemblyParameter.Params.ContainsKey ("InitializeSqlAppDb")) {
          System.Reflection.Assembly assm = System.Reflection.Assembly.GetExecutingAssembly ();
          var initializeSqlFilePath = this._AssemblyParameter.Params["InitializeSqlAppDb"];
          mLogger.Info ($"アプリケーションデータベース初期化SQLファイルを読み込みます path:{initializeSqlFilePath}");
          string sqltext = "";
          using (var stream = assm.GetManifestResourceStream (initializeSqlFilePath)) {
            if (stream == null) {
              throw new ApplicationException ("初期化SQLファイルの読み込みに失敗しました。");
            }
            using (StreamReader reader = new StreamReader (stream)) {
              sqltext = reader.ReadToEnd ();
            }
          }

          @dbc.Database.ExecuteSqlCommand (sqltext);
          @dbc.SaveChanges ();
        }
      }

      mLogger.Info ("アプリケーションデータベースにマイグレーションが必要かチェックします。");
      string currentVersion = apMetadata.Value;
      string nextVersion = currentVersion;
      do {
        currentVersion = nextVersion;
        nextVersion = UpgradeFromResource ("App", currentVersion, @dbc);
        if (nextVersion != currentVersion) isMigrate = true;
      } while (nextVersion != currentVersion);

      if (isMigrate) {
        mLogger.Info ($"アプリケーションデータベースにマイグレーション({@apMetadata.Value}->{@nextVersion})を実施しました。");
        apMetadata.Value = nextVersion;
        @repo.Save ();
      }

      @dbc.SaveChanges ();
      mLogger.Trace ("END");
    }

    /// <summary>
    /// データベースに関する初期化処理
    /// </summary>
    private void InitializeThumbnailDatabase () {
      mLogger.Trace ("IN");
      AppMetaInfo apMetadata = null;
      bool isMigrate = false;

      const string appdb_structure_version_key = "APPDB_VER";

      var @dbc = (ThumbnailDbContext) mContainer.GetInstance<IThumbnailDbContext> (); // DIコンテナがリソースの開放を行う
      bool isInitializeDatabase = false;
      var @repo = new ThumbnailAppMetaInfoRepository (@dbc);
      try {
        apMetadata = @repo.FindBy (p => p.Key == appdb_structure_version_key).FirstOrDefault ();
        if (apMetadata == null) isInitializeDatabase = true;
      } catch (Exception) {
        isInitializeDatabase = true;
      }

      if (isInitializeDatabase) {
        // データベースにテーブルなどの構造を初期化する
        string sqltext = "";
        System.Reflection.Assembly assm = System.Reflection.Assembly.GetExecutingAssembly ();

        using (var stream = assm.GetManifestResourceStream (string.Format ("Foxpict.Service.Web.Assets.Sql.{0}.Initialize_sql.txt", "Thumbnail"))) {
          using (StreamReader reader = new StreamReader (stream)) {
            sqltext = reader.ReadToEnd ();
          }
        }

        mLogger.Info ("SQLファイルから、CREATEを読み込みます");
        @dbc.Database.ExecuteSqlCommand (sqltext);
        @dbc.SaveChanges ();

        apMetadata = @repo.FindBy (p => p.Key == appdb_structure_version_key).FirstOrDefault ();
      }

      if (apMetadata == null) {
        apMetadata = new AppMetaInfo { Key = appdb_structure_version_key, Value = "1.0.0" };
        @repo.Add (apMetadata);

        @repo.Save ();
      }

      string currentVersion = apMetadata.Value;
      string nextVersion = currentVersion;
      do {
        currentVersion = nextVersion;
        nextVersion = UpgradeFromResource ("Thumbnail", currentVersion, @dbc);
        if (nextVersion != currentVersion) isMigrate = true;
      } while (nextVersion != currentVersion);

      if (isMigrate) {
        apMetadata.Value = nextVersion;

        @repo.Save ();
      }

      @dbc.SaveChanges ();
      mLogger.Trace ("OUT");
    }

    /// <summary>
    /// 現在のバージョンからマイグレーションするファイルがリソースファイルにあるか探します。
    /// リソースファイルがある場合はそのファイルに含まれるSQLを実行し、ファイル名からマイグレーション後のバージョンを取得します。
    /// </summary>
    /// <param name="dbselect"></param>
    /// <param name="version"></param>
    /// <param name="dbc"></param>
    /// <returns>次のバージョン番号。マイグレーションを実施しなかった場合は、versionの値がそのまま帰ります。</returns>
    private string UpgradeFromResource (string dbselect, string version, KatalibDbContext @dbc) {
      System.Reflection.Assembly assm = System.Reflection.Assembly.GetExecutingAssembly ();

      string currentVersion = version;
      var mss = assm.GetManifestResourceNames ();

      // この方法で読み込みができるリソースファイルの種類は「埋め込みリソース」を設定したもののみです。
      var r = new Regex (string.Format ("Foxpict.Service.Web.Assets.Sql.{0}.{1}", dbselect, "upgrade - " + currentVersion + "-(.+)\\.txt"));
      foreach (var rf in assm.GetManifestResourceNames ()) {
        var matcher = r.Match (rf);
        if (matcher.Success && matcher.Groups.Count > 1) {
          mLogger.Info ("{0}データベースのアップデート({1} -> {2})", dbselect, version, matcher.Groups[1].Value);
          UpgradeDatabase (rf, @dbc);
          currentVersion = matcher.Groups[1].Value; // 正規表現にマッチした箇所が、マイグレート後のバージョンになります。
        }
      }

      return currentVersion;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="resourcePath"></param>
    /// <param name="dbc">データベース</param>
    private void UpgradeDatabase (string resourcePath, KatalibDbContext dbc) {
      string sqltext = "";
      System.Reflection.Assembly assm = System.Reflection.Assembly.GetExecutingAssembly ();

      using (var stream = assm.GetManifestResourceStream (resourcePath)) {
        using (StreamReader reader = new StreamReader (stream)) {
          sqltext = reader.ReadToEnd ();
        }
      }

      dbc.Database.ExecuteSqlCommand (sqltext);
    }
  }
}
