using Bookstore3.Model.Abstract;
using Dapper.Contrib.Extensions;

namespace Bookstore3.Model;

[Table("shops")]
public class shop : lookup_entity
{

}
