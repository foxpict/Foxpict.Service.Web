using Newtonsoft.Json;
using NLog;
using Foxpict.Service.Infra.Exception;
using Foxpict.Service.Infra.Model;
using Foxpict.Service.Infra.Repository;
using Foxpict.Common.Core;

namespace Foxpict.Service.Web.Builder {
  /// <summary>
  /// レスポンスデータモデル
  /// </summary>
  public class ApiResponseBuilder {
    private static Logger _logger = LogManager.GetCurrentClassLogger ();

    readonly ICategoryRepository mCategoryRepository;

    readonly IContentRepository mContentRepository;

    readonly ILabelRepository mLabelRepository;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="categoryRepository"></param>
    /// <param name="contentRepository"></param>
    /// <param name="labelRepository"></param>
    public ApiResponseBuilder (
      ICategoryRepository categoryRepository,
      IContentRepository contentRepository,
      ILabelRepository labelRepository) {
      this.mCategoryRepository = categoryRepository;
      this.mContentRepository = contentRepository;
      this.mLabelRepository = labelRepository;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="id"></param>
    /// <param name="out_response"></param>
    public void AttachCategoryEntity (long id, ResponseAapi<ICategory> out_response) {
      var category = mCategoryRepository.Load (id);
      if (category != null) {
        out_response.Value = category;

        // 関連データ設定
        out_response.Rel.Add ("labels", JsonConvert.SerializeObject (category.GetLabelList ()));
      } else {
        throw new InterfaceOperationException ("カテゴリが見つかりません");
      }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="id"></param>
    /// <param name="out_respponse"></param>
    public void AttachContentEntity (long id, ResponseAapi<IContent> out_respponse) {
      var content = mContentRepository.Load (id);
      if (content != null) {
        out_respponse.Value = content;
      } else {
        throw new InterfaceOperationException ("コンテント情報が見つかりません");
      }
    }

    /// <summary>
    ///
    /// /// </summary>
    /// <param name="id"></param>
    /// <param name="out_response"></param>
    public void AttachLabelEntity (long id, ResponseAapi<ILabel> out_response) {
      var label = mLabelRepository.Load (id);
      if (label != null) {
        out_response.Value = label;
      } else {
        throw new InterfaceOperationException ("ラベル情報が見つかりません");
      }
    }
  }
}
