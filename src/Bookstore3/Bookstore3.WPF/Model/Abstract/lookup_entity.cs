using Dapper.Contrib.Extensions;
using KpzRepository.Model;

namespace Bookstore3.WPF.Model.Abstract;

public abstract class lookup_entity : BaseEntity<long>
{
    [Key]
    public long id { get; set; }
    public string? name { get; set; } = null;
}