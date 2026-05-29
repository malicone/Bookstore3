using Bookstore3.Model.Abstract;
using Dapper.Contrib.Extensions;

namespace Bookstore3.Model;

[Table("languages")]
public class language : lookup_entity
{

}
