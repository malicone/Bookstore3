using Bookstore3.WPF.Model.Abstract;
using Dapper.Contrib.Extensions;

namespace Bookstore3.WPF.Model;

[Table("publishers")]
public class publisher : lookup_entity
{

}
