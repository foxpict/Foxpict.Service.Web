namespace Foxpict.Service.Web.Model {
  /// <summary>
  ///
  /// </summary>
  public class CategoryParam {
    /// <summary>
    ///
    /// /// </summary>
    public static readonly string LLA_ORDER_NAME_ASC = "NAME_ASC";

    /// <summary>
    ///
    /// </summary>
    public static readonly string LLA_ORDER_NAME_DESC = "NAME_DESC";

    /// <summary>
    ///
    /// </summary>
    /// <value></value>
    public bool IsAlbum { get; set; }

    /// <summary>
    ///
    /// </summary>
    /// <value></value>
    public string lla_order { get; set; }
  }
}
