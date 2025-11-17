using System.Text.Json;
using Avalia_.Data.Models;
using Supabase;
using static Supabase.Postgrest.Constants;   
using SupaClient = Supabase.Client;

namespace Avalia_.Services;

public class SupabaseService
{
    private readonly SupaClient _client;

    public SupabaseService(SupaClient client)
    {
        _client = client;
    }

    public Task InitializeAsync() => _client.InitializeAsync();

    // ---------- Unidades ----------
    public async Task<List<Unidade>> GetUnidadesAsync()
    {
        var resp = await _client
            .From<Unidade>()
            .Filter("is_active", Operator.Equals, "true")
            .Order("nome", Ordering.Ascending)
            .Get();

        return resp.Models;
    }

    public async Task<Unidade?> AddUnidadeAsync(string nome)
    {
        var entidade = new Unidade
        {
            Nome = nome.Trim(),
            IsActive = true
        };

        var resp = await _client
            .From<Unidade>()
            .Insert(entidade); // retorna a linha criada

        return resp.Models.FirstOrDefault();
    }

    // ---------- Funcionários ----------
    public async Task<List<Funcionario>> GetFuncionariosAsync(int idUnidade)
    {
        var resp = await _client
            .From<Funcionario>()
            .Filter("id_unidade", Operator.Equals, idUnidade)
            .Filter("is_active", Operator.Equals, "true")
            .Order("name", Ordering.Ascending)
            .Get();

        return resp.Models;
    }

    public async Task<Funcionario?> AddFuncionarioAsync(string nome, int idUnidade, string? photoUrl = null)
    {
        var f = new Funcionario
        {
            Name = nome.Trim(),
            IdUnidade = idUnidade,
            Photo = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl,
            IsActive = true
        };

        var resp = await _client
            .From<Funcionario>()
            .Insert(f);

        return resp.Models.FirstOrDefault();
    }

    public async Task<string> UploadFotoFuncionarioAsync(
        int idUnidade, int idFuncionario, byte[] fotoBytes, string contentType = "image/jpeg")
    {
        var bucket = _client.Storage.From("funcionarios-fotos");
        var path = $"unidade-{idUnidade}/func-{idFuncionario}/{DateTime.UtcNow:yyyyMMdd-HHmmss}.jpg";

        await bucket.Upload(fotoBytes, path, new Supabase.Storage.FileOptions
        {
            Upsert = false,
            ContentType = contentType
        });

        return bucket.GetPublicUrl(path);
    }

    public async Task UpdateFuncionarioPhotoAsync(int idFuncionario, string photoUrl)
    {
        await _client
            .From<Funcionario>()
            .Filter("id", Operator.Equals, idFuncionario)
            .Set(f => f.Photo, photoUrl)   
            .Update();                      
    }

    // ---------- Avaliações (leitura) ----------
    public async Task<List<Avaliacao>> GetUltimasAvaliacoesAsync(
        int idUnidade, DateTime? deUtc = null, DateTime? ateUtc = null, int limit = 100)
    {
        var query = _client.From<Avaliacao>()
            .Filter("id_unidade", Operator.Equals, idUnidade)
            .Order("criado_em", Ordering.Descending)
            .Limit(limit);

        if (deUtc.HasValue)
            query = query.Filter("criado_em", Operator.GreaterThanOrEqual, deUtc.Value.ToString("O"));
        if (ateUtc.HasValue)
            query = query.Filter("criado_em", Operator.LessThan, ateUtc.Value.ToString("O"));

        var resp = await query.Get();
        return resp.Models;
    }

    public async Task<List<Avaliacao>> GetAvaliacoesAsync(
    int? idUnidade = null, int? idFuncionario = null,
    DateTime? deUtc = null, DateTime? ateUtc = null, int limit = 2000)
    {
        var q = _client.From<Avaliacao>().Order("criado_em", Ordering.Descending).Limit(limit);

        if (idUnidade.HasValue)
            q = q.Filter("id_unidade", Operator.Equals, idUnidade.Value);

        if (idFuncionario.HasValue)
            q = q.Filter("id_funcionario", Operator.Equals, idFuncionario.Value);

        if (deUtc.HasValue)
            q = q.Filter("criado_em", Operator.GreaterThanOrEqual, deUtc.Value.ToString("O"));

        if (ateUtc.HasValue)
            q = q.Filter("criado_em", Operator.LessThanOrEqual, ateUtc.Value.ToString("O"));

        var resp = await q.Get();
        return resp.Models;
    }

    // ---------- Inserir avaliação (via RPC) ----------
    public async Task<Avaliacao?> AddAvaliacaoAsync(
        int idUnidade, int? idFuncionario, short emojiScore, short nota, string? comentario, DateTime? criadoEmUtc = null)
    {
        var args = new Dictionary<string, object?>
        {
            ["p_id_unidade"] = idUnidade,
            ["p_id_funcionario"] = idFuncionario,
            ["p_emoji_score"] = emojiScore,
            ["p_nota"] = nota,
            ["p_comentario"] = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim(),
            ["p_criado_em"] = criadoEmUtc?.ToString("O")
        };

        var res = await _client.Rpc("add_avaliacao", args);

        // 'res.Content' vem como JSON (array com 1 item retornado pelo "returning *")
        if (!string.IsNullOrWhiteSpace(res.Content))
        {
            using var doc = JsonDocument.Parse(res.Content);
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var first = doc.RootElement[0];
                if (first.TryGetProperty("id", out var idProp) && idProp.TryGetInt64(out var id))
                {
                    var lookup = await _client
                        .From<Avaliacao>()
                        .Filter("id", Operator.Equals, id)
                        .Get();

                    return lookup.Models.FirstOrDefault();
                }
            }
        }

        return null;
    }

    // ---------- Funcionários (update/delete) ----------
    public async Task<Funcionario?> UpdateFuncionarioAsync(int idFuncionario, string nome, int idUnidade, string? photoUrl)
    {
        var resp = await _client
            .From<Funcionario>()
            .Filter("id", Operator.Equals, idFuncionario)
            .Set(f => f.Name, nome.Trim())
            .Set(f => f.IdUnidade, idUnidade)
            .Set(f => f.Photo, string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl)
            .Update();

        return resp.Models.FirstOrDefault();
    }

    public async Task<bool> DeleteFuncionarioAsync(int idFuncionario)
    {
        // soft delete: is_active = false
        var resp = await _client
            .From<Funcionario>()
            .Filter("id", Operator.Equals, idFuncionario)
            .Set(f => f.IsActive, false)
            .Update();

        return resp.Models.Any();
    }

    // ---------- Unidades (update/delete) ----------
    public async Task<Unidade?> UpdateUnidadeAsync(int idUnidade, string nome)
    {
        var resp = await _client
            .From<Unidade>()
            .Filter("id_unidade", Operator.Equals, idUnidade)
            .Set(u => u.Nome, nome.Trim())
            .Update();

        return resp.Models.FirstOrDefault();
    }

    public async Task<bool> DeleteUnidadeAsync(int idUnidade)
    {
        // Soft delete: marca como inativa
        var resp = await _client
            .From<Unidade>()
            .Filter("id_unidade", Operator.Equals, idUnidade)
            .Set(u => u.IsActive, false)
            .Update();

            return resp.Models.Any();
    }



    // ---------- Storage ----------
    // From(...) retorna IStorageFileApi<FileObject>
    public Supabase.Storage.Interfaces.IStorageFileApi<Supabase.Storage.FileObject> FromBucket(string bucket)
        => _client.Storage.From(bucket);

    public SupaClient RawClient => _client;
}
