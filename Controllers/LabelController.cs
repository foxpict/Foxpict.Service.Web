using System;
using System.Collections.Generic;
using System.Linq;
using Foxpict.Common.Core;
using Foxpict.Service.Infra.Exception;
using Foxpict.Service.Infra.Model;
using Foxpict.Service.Infra.Repository;
using Foxpict.Service.Model;
using Foxpict.Service.Web.Builder;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Foxpict.Service.Web.Controllers {
  /// <summary>
  /// ラベル操作コントローラ
  /// </summary>
  [Produces ("application/json")]
  [Route ("aapi/[controller]")]
  [ApiController]
  public class LabelController : Controller {
    private static Logger mLogger = LogManager.GetCurrentClassLogger ();

    readonly ApiResponseBuilder mBuilder;

    readonly ILabelRepository mLabelRepository;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="labelRepository"></param>
    public LabelController (ApiResponseBuilder builder, ILabelRepository labelRepository) {
      this.mBuilder = builder;
      this.mLabelRepository = labelRepository;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    [HttpGet ()]
    public ResponseAapi<ICollection<ILabel>> GetLabel ([FromQuery] RequestParamGetLabel requestParam) {
      // TODO: オフセット値を使用したデータ取得
      mLogger.Info ("Parameter Offset:{}", requestParam.Offset);

      List<ILabel> labels = new List<ILabel> ();
      var response = new ResponseAapi<ICollection<ILabel>> ();
      try {
        foreach (var prop in mLabelRepository.GetAll ()) {
          labels.Add (prop);
        }
        response.Value = labels;
      } catch (Exception expr) {
        mLogger.Error (expr.Message);
        throw new InterfaceOperationException ();
      }

      return response;
    }

    /// <summary>
    /// 任意のラベルを取得します
    /// すべてのリンクIDを返すのでコストが大きい。
    /// </summary>
    /// <param name="id">ラベルID</param>
    /// <returns></returns>
    [HttpGet ("{id}")]
    [ProducesResponseType (200)]
    public ActionResult<ResponseAapi<ILabel>> GetLabel (int id) {
      var response = new ResponseAapi<ILabel> ();
      mBuilder.AttachLabelEntity (id, response);

      var label = response.Value;

      // リンクデータの生成
      response.Link.Add ("category-list", label.GetCategoryList ().OrderBy (prop => prop.Name).Select (prop => prop.Id).ToArray ());

      response.Link.Add ("content-list", label.GetContentList ().OrderBy (prop => prop.Name).Select (prop => prop.Id).ToArray ());

      var ccQuery = this.mLabelRepository.FindChildren (label.Id);
      response.Link.Add ("children", ccQuery.Select (prop => prop.Id).ToArray ());

      return response;
    }

    /// <summary>
    /// リンクデータに含まれるカテゴリ一覧を取得します
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet ("{id}/l/category-list")]
    [ProducesResponseType (200)]
    public ActionResult<ResponseAapi<ICollection<ICategory>>> GetLabelLink_CategoryList (int id) {
      // TODO: すべての要素を返さずに、ページングで特定範囲の取得に対応する
      var categoryList = new List<ICategory> ();
      var label = this.mLabelRepository.Load (id);
      categoryList.AddRange (
        label.GetCategoryList ().Take (1000000) // 本来はEnumerationを返す
      );

      var response = new ResponseAapi<ICollection<ICategory>> {
        Value = categoryList
      };

      return response;
    }

    /// <summary>
    /// リンクデータに含まれるコンテント一覧を取得します
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <response code="200">ラベルと関連付けられたコンテント一覧を取得しました</response>
    [HttpGet ("{id}/l/content-list")]
    [ProducesResponseType (200)]
    public ActionResult<ResponseAapi<ICollection<IContent>>> GetLabelLink_ContentList (int id) {
      // TODO: すべての要素を返さずに、ページングで特定範囲の取得に対応する
      var contentList = new List<IContent> ();
      var label = this.mLabelRepository.Load (id);
      contentList.AddRange (
        label.GetContentList ().Take (1000000) // 本来はEnumerationを返す
      );

      var response = new ResponseAapi<ICollection<IContent>> {
        Value = contentList
      };

      return response;
    }

    /// <summary>
    /// リンクデータに含まれる子階層ラベル一覧を取得します
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet ("{id}/l/children")]
    [ProducesResponseType (200)]
    public ActionResult<ResponseAapi<ICollection<ILabel>>> GetLabelLink_Children (int id) {
      ResponseAapi<ICollection<ILabel>> response;
      if (id == 0) {
        response = new ResponseAapi<ICollection<ILabel>> {
        Value = this.mLabelRepository.FindRoot ().Take (100000).ToList ()
        };
      } else {
        response = new ResponseAapi<ICollection<ILabel>> {
          Value = this.mLabelRepository.FindChildren (id).Take (100000).ToList ()
        };
      }
      return response;
    }

    /// <summary>
    /// 任意ラベルの子階層カテゴリを取得します
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="link_id"></param>
    /// <response code="200">カテゴリと関連付けられた子階層カテゴリを取得しました</response>
    /// <response code="400">指定した項目が取得できませんでした</response>
    [HttpGet ("{id}/children/{link_id}")]
    [ProducesResponseType (200)]
    [ProducesResponseType (400)]
    public ActionResult<ResponseAapi<ICategory>> GetCategoryLink_cc (int id, int link_id) {
      mLogger.Info ("REQUEST - {0}/cc/{1}", id, link_id);
      var response = new ResponseAapi<ICategory> ();

      var linkedCategory = this.mLabelRepository.FindChildren (id).Where (prop => prop.Id == link_id).SingleOrDefault ();
      if (linkedCategory != null) {
        mBuilder.AttachCategoryEntity (link_id, response);

        var sub = this.mLabelRepository.FindChildren (linkedCategory.Id).FirstOrDefault ();
        if (sub != null) {
          response.Link.Add ("cc_available", true);
        }
      } else {
        return NotFound ();
      }

      return response;
    }

    /// <summary>
    /// ラベルリンクデータ情報取得API(カテゴリ情報)
    /// ※削除予定
    /// </summary>
    /// <returns></returns>
    [HttpGet ("{query}/category")]
    public ResponseAapi<ICollection<ICategory>> GetLabelLinkCategory (string query) {
      // TODO: queryは、整数のみ（他の入力形式は、将来実装)
      var response = new ResponseAapi<ICollection<ICategory>> ();

      try {
        // 現設計では、queryで指定できるラベル情報は1つのみであるため、
        // そのラベルを読み込んで、ラベル情報に関連付けされているカテゴリ情報の一覧を返す。
        long labelId = long.Parse (query);
        var label = mLabelRepository.Load (labelId) as Label;
        if (label != null)
          response.Value = label.Categories.Select (prop => (ICategory) prop.Category).ToList ();
        else
          throw new ApplicationException (string.Format ("指定したラベル情報(ID:{0})の読み込みに失敗しました", labelId));
      } catch (Exception expr) {
        mLogger.Error (expr.Message);
        throw new InterfaceOperationException ();
      }

      return response;
    }

    /// <summary>
    /// ラベルリンクデータ情報取得API(コンテント情報)
    /// ※削除予定
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    [HttpGet ("{query}/content")]
    public ResponseAapi<ICollection<IContent>> GetLabelLinkContent (string query) {
      throw new NotImplementedException ();
    }

    /// <summary>
    ///
    /// /// </summary>
    public class RequestParamGetLabel {
      /// <summary>
      ///
      /// </summary>
      /// <value></value>
      public int Offset { get; set; }
    }
  }
}
