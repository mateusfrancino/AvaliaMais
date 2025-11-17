using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Avalia_.Data.Models;

[Table("funcionarios")]
public class Funcionario : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("id_unidade")]
    public int IdUnidade { get; set; }

    [Column("photo")]
    public string? Photo { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
