using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Avalia_.Data.Models;

[Table("avaliacoes")]
public class Avaliacao : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("id_unidade")]
    public int IdUnidade { get; set; }

    [Column("id_funcionario")]
    public int? IdFuncionario { get; set; }

    [Column("emoji_score")]
    public short EmojiScore { get; set; } // 1..5

    [Column("nota")]
    public short Nota { get; set; }       // 1..5

    [Column("comentario")]
    public string? Comentario { get; set; }

    [Column("criado_em")]
    public DateTimeOffset CriadoEm { get; set; } // timestamptz
}
