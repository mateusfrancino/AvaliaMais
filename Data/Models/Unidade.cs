using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Avalia_.Data.Models;

[Table("unidades")]
public class Unidade : BaseModel
{
    [PrimaryKey("id_unidade", false)]
    public int IdUnidade { get; set; }

    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
