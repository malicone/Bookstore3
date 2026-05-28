using Bookstore3.WPF.Model.Abstract;
using Dapper.Contrib.Extensions;

namespace Bookstore3.WPF.Model;

[Table("languages")]
public class language : lookup_entity
{

}
