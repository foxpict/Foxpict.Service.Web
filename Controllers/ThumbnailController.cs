using System;
using System.Linq;
using Foxpict.Service.Infra.Model;
using Foxpict.Service.Infra.Repository;
using Foxpict.Service.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Foxpict.Service.Web.Controllers {
  /// <summary>
  /// サムネイル操作関連APIコントローラ
  /// </summary>
  [Route ("aapi/[controller]")]
  public class ThumbnailController : Controller {
    IThumbnailRepository thumbnailRepository;

    /// <summary>
    /// コンストラクタ
    /// /// </summary>
    /// <param name="thumbnailRepository"></param>
    public ThumbnailController (IThumbnailRepository thumbnailRepository) {
      this.thumbnailRepository = thumbnailRepository;
    }

    /// <summary>
    /// サムネイル画像データを取得する
    /// </summary>
    /// <param name="thumbnailKey">サムネイルハッシュ、またはサムネイル情報キー</param>
    /// <returns>サムネイル画像のバイナリデータ</returns>
    [HttpGet ("{thumbnailKey}")]
    public IActionResult FetchThumbnail (string thumbnailKey) {
      long thumbnailId = 0L;
      IThumbnail thumbnail = null;
      try {
        if (long.TryParse (thumbnailKey, out thumbnailId)) {
          thumbnail = thumbnailRepository.Load (thumbnailId);
        } else {
          thumbnail = thumbnailRepository.FindByKey (thumbnailKey).FirstOrDefault ();
        }
      } catch (Exception) {
        // サムネイルが正常に取得できない場合は、常にnullとする。
        thumbnail = null;
      }

      // リソースの有効期限等を決定する
      //DateTimeOffset now = DateTime.Now;
      //var etag = new EntityTagHeaderValue("\"" + Guid.NewGuid().ToString() + "\"");

      if (thumbnail == null) {
        System.Reflection.Assembly assm = System.Reflection.Assembly.GetExecutingAssembly ();
        string filePath = string.Format ("Foxpict.Service.Web.Assets.Icon.UI.Article3.png");
        return new FileStreamResult (assm.GetManifestResourceStream (filePath), "image/png");
      }

      return new FileContentResult (thumbnail.BitmapBytes, thumbnail.MimeType);
    }
  }
}
