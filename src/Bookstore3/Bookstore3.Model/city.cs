using Bookstore3.Model.Abstract;
using Dapper.Contrib.Extensions;

namespace Bookstore3.Model;

[Table("cities")]
public class city : lookup_entity
{

}
