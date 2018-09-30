using System;
using System.IO;
using Foxpict.Common.Core;
using Foxpict.Service.Infra.Exception;
using Foxpict.Service.Infra.Model;
using Foxpict.Service.Infra.Repository;
using Foxpict.Service.Model;
using Foxpict.Service.Web.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using NLog;

namespace Foxpict.Service.Web.Controllers {
  /// <summary>
  /// コンテント情報コントローラー
  /// </summary>
  [Route ("aapi/[controller]")]
  public class ArtifactController : Controller {
    private static Logger _logger = LogManager.GetCurrentClassLogger ();

    readonly ApiResponseBuilder mBuilder;

    readonly IContentRepository mContentRepository;

    readonly IFileMappingInfoRepository mFileMappingInfoRepository;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="contentRepository"></param>
    /// <param name="fileMappingInfoRepository"></param>
    public ArtifactController (
      ApiResponseBuilder builder,
      IContentRepository contentRepository,
      IFileMappingInfoRepository fileMappingInfoRepository
    ) {
      this.mBuilder = builder;
      this.mContentRepository = contentRepository;
      this.mFileMappingInfoRepository = fileMappingInfoRepository;
    }

    /// <summary>
    /// コンテント詳細情報取得API
    /// </summary>
    /// <remarks>
    /// GET aapi/artifact/{id}
    /// </remarks>
    /// <param name="id">コンテントID</param>
    /// <returns></returns>
    [HttpGet ("{id}")]
    public ResponseAapi<IContent> GetContent (int id) {
      var response = new ResponseAapi<IContent> ();
      try {
        mBuilder.AttachContentEntity (id, response);
        var content = response.Value;

        // リンクデータ
        // "category"
        if (content.GetCategory () != null)
          response.Link.Add ("category", content.GetCategory ().Id);
      } catch (Exception expr) {
        _logger.Error (expr.Message);
        throw new InterfaceOperationException ();
      }
      return response;
    }

    /// <summary>
    /// コンテント情報のリンクデータ(所属カテゴリ情報)を取得します
    /// </summary>
    /// <param name="id">コンテント情報のキー</param>8
    /// <returns>所属カテゴリ情報</returns>
    [HttpGet ("{id}/category")]
    public ResponseAapi<ICategory> GetContentLink_Category (int id) {
      var response = new ResponseAapi<ICategory> ();
      try {
        var content = mContentRepository.Load (id);
        mBuilder.AttachCategoryEntity (content.GetCategory ().Id, response);
      } catch (Exception expr) {
        _logger.Error (expr.Message);
        throw new InterfaceOperationException ();
      }
      return response;
    }

    /// <summary>
    /// プレビューファイルを取得します
    /// </summary>
    /// <param name="id">コンテントID</param>
    /// <returns>コンテントのプレビューファイル</returns>
    [HttpGet ("{id}/preview")]
    public IActionResult FetchPreviewFile (int id) {
      _logger.Debug ("IN");
      var content = mContentRepository.Load (id);
      if (content == null) throw new InterfaceOperationException ("コンテント情報が見つかりません");

      var fmi = content.GetFileMappingInfo ();
      if (fmi == null) throw new InterfaceOperationException ("ファイルマッピング情報が見つかりません1");

      var efmi = mFileMappingInfoRepository.Load (fmi.Id);
      if (efmi == null) throw new InterfaceOperationException ("ファイルマッピング情報が見つかりません2");

      // NOTE: リソースの有効期限等を決定する
      DateTimeOffset now = DateTime.Now;
      var etag = new EntityTagHeaderValue ("\"" + Guid.NewGuid ().ToString () + "\"");
      string filePath = Path.Combine (efmi.GetWorkspace ().PhysicalPath, efmi.MappingFilePath);
      var file = PhysicalFile (
        Path.Combine (efmi.GetWorkspace ().PhysicalPath, efmi.MappingFilePath), efmi.Mimetype, now, etag);

      _logger.Debug ("OUT");
      return file;
    }

    /// <summary>
    /// コンテント情報を永続化します
    /// </summary>
    /// <param name="id">更新対象のコンテントID</param>
    /// <param name="content">更新オブジェクト</param>
    /// <returns></returns>
    [HttpPut ("{id}/a")]
    public ResponseAapi<Boolean> UpdateContent (long id, [FromBody] Content content) {
      _logger.Debug ("IN");
      var response = new ResponseAapi<Boolean> ();

      var targetContent = mContentRepository.Load (id);
      if (content == null) throw new InterfaceOperationException ("コンテント情報が見つかりません");

      targetContent.ArchiveFlag = content.ArchiveFlag;
      targetContent.Caption = content.Caption;
      targetContent.Comment = content.Comment;
      targetContent.StarRating = content.StarRating;
      targetContent.Name = content.Name;

      mContentRepository.Save ();
      response.Value = true;
      _logger.Debug ("OUT");
      return response;
    }

    /// <summary>
    /// コンテントのステータス更新処理を実行する
    /// </summary>
    /// <param name="id">更新対象のコンテントID</param>
    /// <param name="operation">更新処理種別</param>
    /// <returns>更新処理が正常終了した場合はtrue</returns>
    [HttpPut ("{id}/exec/{operation}")]
    public ResponseAapi<Boolean> UpdateContentStatus (long id, string operation) {
      _logger.Debug ("IN");
      var response = new ResponseAapi<Boolean> ();

      var content = mContentRepository.Load (id);
      if (content == null) throw new InterfaceOperationException ("コンテント情報が見つかりません");

      bool result = false;
      switch (operation) {
        case "read":
          if (!content.ReadableFlag) {
            content.ReadableFlag = true;
            content.ReadableDate = DateTime.Now;
          }
          content.LastReadDate = DateTime.Now;
          content.ReadableCount = content.ReadableCount + 1;
          result = true;
          break;
        default:
          _logger.Warn ($"不明なオペレーション({@operation})です。");
          break;
      }

      if (result) {
        mContentRepository.Save ();
      }

      response.Value = result;
      _logger.Debug ("OUT");
      return response;
    }
  }
}
