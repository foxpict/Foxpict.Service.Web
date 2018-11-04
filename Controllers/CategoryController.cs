using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foxpict.Common.Core;
using Foxpict.Service.Extention.Sdk;
using Foxpict.Service.Infra.Exception;
using Foxpict.Service.Infra.Model;
using Foxpict.Service.Infra.Repository;
using Foxpict.Service.Model;
using Foxpict.Service.Web.Builder;
using Foxpict.Service.Web.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NLog;

namespace Foxpict.Service.Web.Controllers {
  /// <summary>
  /// カテゴリ操作コントローラ
  /// </summary>
  [Produces ("application/json")]
  [Route ("aapi/[controller]")]
  [ApiController]
  public class CategoryController : Controller {
    private readonly Logger mLogger;

    private readonly ApiResponseBuilder mBuilder;

    private readonly ICategoryRepository mCategoryRepository;

    private readonly IContentRepository mContentRepository;

    private readonly ExtentionManager mExtentionManager;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="extentionManager"></param>
    /// <param name="categoryRepository"></param>
    /// <param name="contentRepository"></param>
    public CategoryController (ApiResponseBuilder builder, ExtentionManager extentionManager, ICategoryRepository categoryRepository, IContentRepository contentRepository) {
      this.mLogger = LogManager.GetCurrentClassLogger ();
      this.mBuilder = builder;
      this.mCategoryRepository = categoryRepository;
      this.mExtentionManager = extentionManager;
      this.mContentRepository = contentRepository;
    }

    /// <summary>
    /// 任意のカテゴリを取得します
    /// </summary>
    /// <param name="id"></param>
    /// <param name="param"></param>
    [HttpGet ("{id}")]
    [ProducesResponseType (200)]
    public ActionResult<ResponseAapi<ICategory>> GetCategory (int id, [FromQuery] CategoryParam param) {
      var response = new ResponseAapi<ICategory> ();

      mBuilder.AttachCategoryEntity (id, response);
      var category = response.Value;

      // リンクデータの生成
      // "la"
      if (param.lla_order == CategoryParam.LLA_ORDER_NAME_ASC) {
        response.Link.Add ("la", category.GetContentList ().OrderBy (prop => prop.Name).Select (prop => prop.Id).ToArray ());
      } else if (param.lla_order == CategoryParam.LLA_ORDER_NAME_DESC) {
        response.Link.Add ("la", category.GetContentList ().OrderByDescending (prop => prop.Name).Select (prop => prop.Id).ToArray ());
      } else {
        response.Link.Add ("la", category.GetContentList ().Select (prop => prop.Id).ToArray ());
      }

      // "cc"
      var ccQuery = this.mCategoryRepository.FindChildren (category);
      response.Link.Add ("cc", ccQuery.Select (prop => prop.Id).ToArray ());

      // 拡張機能の呼び出し
      this.mExtentionManager.Execute (ExtentionCutpointType.API_GET_CATEGORY, category);

      return response;
    }

    /// <summary>
    /// カテゴリの親階層カテゴリを取得します
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <response code="200">カテゴリと関連付けられた親階層カテゴリを取得しました</response>
    /// <response code="400">指定した項目が取得できませんでした</response>
    [HttpGet ("{id}/pc")]
    [ProducesResponseType (200)]
    [ProducesResponseType (400)]
    public ActionResult<ResponseAapi<ICategory>> GetCategoryLink_pc (int id) {
      var response = new ResponseAapi<ICategory> {
        Value = GetCategoryLink (id, "pc").FirstOrDefault ()
      };

      if (response.Value == null) {
        return NotFound ();
      }

      return response;
    }

    /// <summary>
    /// カテゴリに含まれる子階層カテゴリ一覧を取得します
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200">カテゴリと関連付けられた子階層カテゴリ一覧を取得しました</response>
    [HttpGet ("{id}/cc")]
    [ProducesResponseType (200)]
    public ActionResult<ResponseAapi<ICollection<ICategory>>> GetCategoryLink_cc (int id) {
      var response = new ResponseAapi<ICollection<ICategory>> {
        Value = GetCategoryLink (id, "cc")
      };

      return response;
    }

    /// <summary>
    /// カテゴリに含まれるコンテント一覧を取得します
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200">カテゴリと関連付けられたコンテント一覧を取得しました</response>
    [HttpGet ("{id}/la")]
    [ProducesResponseType (200)]
    public ActionResult<ResponseAapi<ICollection<IContent>>> GetCategoryLink_la (int id) {
      mLogger.Info ("REQUEST - {0}", id);

      var categoryList = new List<IContent> ();
      var response = new ResponseAapi<ICollection<IContent>> ();

      var category = this.mCategoryRepository.Load (id);
      categoryList.AddRange (
        category.GetContentList ().OrderBy (prop => prop.Name).Select (prop => prop).Take (100000)
      );
      response.Value = categoryList;
      return response;
    }

    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    ///    GET api/category/{id}/albc/{link_id}
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="link_id"></param>
    /// <returns></returns>
    [HttpGet ("{id}/albc/{link_id}")]
    public ResponseAapi<Category> GetCategoryLink_albc (int id, int link_id) {
      mLogger.Info ("REQUEST - {0}/albc/{1}", id, link_id);

      var response = new ResponseAapi<Category> ();
      response.Value = new Category { Id = link_id, Name = "リンクカテゴリ " + link_id };
      return response;
    }

    /// <summary>
    /// カテゴリと関連付けされた子階層カテゴリを取得します
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="link_id"></param>
    /// <response code="200">カテゴリと関連付けられた子階層カテゴリを取得しました</response>
    /// <response code="400">指定した項目が取得できませんでした</response>
    [HttpGet ("{id}/cc/{link_id}")]
    [ProducesResponseType (200)]
    [ProducesResponseType (400)]
    public ActionResult<ResponseAapi<ICategory>> GetCategoryLink_cc (int id, int link_id) {
      mLogger.Info ("REQUEST - {0}/cc/{1}", id, link_id);
      var response = new ResponseAapi<ICategory> ();

      var linkedCategory = this.mCategoryRepository.FindChildren (id).Where (prop => prop.Id == link_id).SingleOrDefault ();
      if (linkedCategory != null) {
        mBuilder.AttachCategoryEntity (link_id, response);

        var sub = this.mCategoryRepository.FindChildren (linkedCategory).FirstOrDefault ();
        if (sub != null) {
          response.Link.Add ("cc_available", true);
        }
      } else {
        return NotFound ();
      }

      return response;
    }

    /// <summary>
    /// カテゴリと関連付けされたコンテントを取得します
    /// </summary>
    /// <remarks>
    ///    コンテント情報取得と同じ情報量を返します。
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="link_id"></param>
    /// <response code="200">カテゴリと関連付けられたコンテントを取得しました</response>
    /// <response code="400">指定した項目が取得できませんでした</response>
    [HttpGet ("{id}/la/{link_id}")]
    [ProducesResponseType (200)]
    [ProducesResponseType (400)]
    public ActionResult<ResponseAapi<IContent>> GetCategoryLink_la (int id, int link_id) {
      mLogger.Info ("REQUEST - {0}/la/{1}", id, link_id);

      var response = new ResponseAapi<IContent> ();
      var linkedContent = this.mCategoryRepository.Load (id).GetContentList ().Where (prop => prop.Id == link_id).SingleOrDefault ();

      if (linkedContent != null) {
        mBuilder.AttachContentEntity (link_id, response);
      } else {
        return NotFound ();
      }

      return response;
    }

    /// <summary>
    /// ラベルの組み合わせすべてに一致するアルバムカテゴリ一覧を取得します
    /// </summary>
    /// <param name="labels">ラベル組み合わせ</param>
    /// <returns>アルバムカテゴリ一覧</returns>
    [HttpGet ("w_album/+{labels}")]
    [ProducesResponseType (200)]
    public ActionResult<ResponseAapi<ICollection<ICategory>>> SearchAlbumCategory1 (string labels) {
      mLogger.Info ("REQUEST - {0}", labels);
      var response = new ResponseAapi<ICollection<ICategory>> ();
      var categoryList = new List<ICategory> ();

      // クエリパラメータからラベルIDを取り出します
      List<ILabel> labelentity = new List<ILabel> ();
      string[] labelIdstr = labels.Split (',');
      foreach (var labelId in labelIdstr) {
        labelentity.Add (
          new Label { Id = long.Parse (labelId) }
        );
      }
      var query = this.mCategoryRepository.FindCategory (labelentity)
        .Where (category => category.AlbumFlag == true);
      categoryList.AddRange (
        query.Take (1000000)
      );

      response.Value = categoryList;
      return response;
    }

    /// <summary>
    /// ラベルの組み合わせすべてに一致するアルバムカテゴリ一覧を取得します
    /// </summary>
    /// <param name="labels">ラベル組み合わせ</param>
    /// <returns>アルバムカテゴリ一覧</returns>
    [HttpGet ("w_album/-{labels}")]
    [ProducesResponseType (200)]
    public ActionResult<ResponseAapi<ICollection<ICategory>>> SearchAlbumCategory2 (string labels) {
      mLogger.Info ("REQUEST - {0}", labels);
      var response = new ResponseAapi<ICollection<ICategory>> ();
      var categoryList = new List<ICategory> ();

      // クエリパラメータからラベルIDを取り出します
      List<ILabel> labelentity = new List<ILabel> ();
      string[] labelIdstr = labels.Split (',');
      foreach (var labelId in labelIdstr) {
        labelentity.Add (
          new Label { Id = long.Parse (labelId) }
        );
      }
      var query = this.mCategoryRepository.FindCategoryOr (labelentity)
        .Where (category => category.AlbumFlag == true);
      categoryList.AddRange (
        query.Take (1000000)
      );

      response.Value = categoryList;
      return response;
    }

    /// <summary>
    /// 新しいカテゴリを登録します（未実装）
    /// </summary>
    /// <param name="value"></param>
    [HttpPost]
    public void Post ([FromBody] string value) { }

    /// <summary>
    /// カテゴリの表示回数を更新します
    /// </summary>
    /// <param name="id">更新対象のカテゴリを示すキー</param>
    /// <response code="200">カテゴリの表示回数を更新しました</response>
    /// <response code="400">指定した項目が取得できませんでした</response>
    [HttpPut ("{id}/read")]
    [ProducesResponseType (200)]
    [ProducesResponseType (400)]
    public ActionResult PutReadableCategory (int id) {
      mLogger.Info ("REQUEST - {0}/read", id);

      var category = mCategoryRepository.Load (id);
      if (category != null) {
        if (!category.ReadableFlag) {
          category.ReadableFlag = true;
          category.ReadableDate = DateTime.Now;
        }
        category.LastReadDate = DateTime.Now;
        category.ReadableCount = category.ReadableCount + 1;
        mCategoryRepository.Save ();
        return Ok ();
      } else {
        return NotFound ();
      }
    }

    /// <summary>
    /// カテゴリのパラメータを更新します
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="id">更新対象のカテゴリを示すキー</param>
    /// <param name="value">カテゴリの更新対象のパラメータが含まれるJSON文字列</param>
    [HttpPut ("{id}")]
    public void PutCategoryProp (int id, [FromBody] string value) {
      mLogger.Info ("REQUEST - {0} Body={1}", id, value);

      mCategoryRepository.UpdatePopulateFromJson (id, value);
      mCategoryRepository.Save ();
    }

    /// <summary>
    /// カテゴリを削除します（未実装）
    /// </summary>
    /// <param name="id"></param>
    [HttpDelete ("{id}")]
    public void Delete (int id) { }

    /// <summary>
    /// カテゴリ情報リンク取得
    /// </summary>
    /// <param name="id"></param>
    /// <param name="link_type">リンクタイプを指定します。</param>
    /// <remarks>
    /// GET api/category/{id}/{link_type}
    /// link_type =
    /// "pc" : 親階層のカテゴリ情報を取得します
    /// "cc" : 子階層のカテゴリ情報リストを取得します。
    /// </remarks>
    /// <returns></returns>
    private ICollection<ICategory> GetCategoryLink (int id, string link_type) {
      var categoryList = new List<ICategory> ();

      if (link_type == "pc") {
        var category = this.mCategoryRepository.Load (id);
        var parentCategory = this.mCategoryRepository.Load (category.GetParentCategory ().Id);
        if (parentCategory != null) categoryList.Add (parentCategory);
      } else if (link_type == "cc") {
        var category = this.mCategoryRepository.Load (id);
        categoryList.AddRange (
          this.mCategoryRepository.FindChildren (category).Take (1000000)
        );
      }

      return categoryList;
    }

  }
}
