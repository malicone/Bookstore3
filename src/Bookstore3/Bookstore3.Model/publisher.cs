using Bookstore3.Model.Abstract;
using Dapper.Contrib.Extensions;

namespace Bookstore3.Model;

[Table("publishers")]
public class publisher : lookup_entity
{

}
