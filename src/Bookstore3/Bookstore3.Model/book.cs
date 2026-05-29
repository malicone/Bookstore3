using Dapper.Contrib.Extensions;
using KpzRepository.Model;

namespace Bookstore3.Model;

[Table("books")]
public class book : BaseEntity<long>
{
    [Key]
    public long id { get; set; }

    public DateTime crt_date_time { get; set; }

    public string? author { get; set; }

    public string title { get; set; } = null!;

    public long publisher_id { get; set; }

    public int? page_count { get; set; }

    public int? publish_year { get; set; }

    public int edition { get; set; } = 1;

    public string? format { get; set; }

    public string? isbn { get; set; }

    public double? price { get; set; }

    public DateTime? date_when_get { get; set; }

    public bool wrapper { get; set; }

    public long language_id { get; set; }

    public long? group_id { get; set; }

    public long shop_id { get; set; }

    public long city_id { get; set; }

    public bool has_digit_copy { get; set; }

    public string? annotation { get; set; }

    public string? details { get; set; }

    public byte[]? cover_image { get; set; }

    public string? book_file { get; set; }
}