using Dapper.Contrib.Extensions;
using KpzRepository.Model;

namespace Bookstore3.Model;

[Table("app_options")]
public class app_option : BaseEntity<string>
{
    [ExplicitKey]
    public string option_name { get; set; } = string.Empty;
    public string? option_value { get; set; }
}
