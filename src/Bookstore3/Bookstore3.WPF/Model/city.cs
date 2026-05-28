using Bookstore3.WPF.Model.Abstract;
using Dapper.Contrib.Extensions;

namespace Bookstore3.WPF.Model;

[Table("cities")]
public class city : lookup_entity
{

}
