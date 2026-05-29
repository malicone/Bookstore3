using Bookstore3.Model.Abstract;
using Dapper.Contrib.Extensions;

namespace Bookstore3.Model;

[Table("groups")]
public class group : lookup_entity
{

}
