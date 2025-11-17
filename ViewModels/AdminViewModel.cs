using Avalia_.Data.Models;
using Avalia_.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public partial class AdminViewModel : ObservableObject
{
    // ===== Records / Itens da UI =====
    public record CsatUnidade(string Nome, double Csat, string Cor);
    public record EmojiBucket(string Emoji, int Quantidade, string Cor);
    public record Unidade(string Id, string Nome);
    public record Funcionario(string Id, string Nome);

    public class AvaliacaoItem
    {
        public string Emoji { get; init; } = "🙂";
        public string Comentario { get; init; } = "";
        public DateTime Data { get; init; }
        public string Unidade { get; init; } = "";
        public string DataResumo => Data.ToString("dd/MM/yyyy HH:mm");
    }

    // ===== DI / Estado =====
    private readonly SupabaseService _supa;
    private readonly SemaphoreSlim _gate = new(1, 1); // evita concorrência

    public ObservableCollection<CsatUnidade> CsatPorUnidade { get; } = new();
    public ObservableCollection<EmojiBucket> DistribEmoji { get; } = new();
    public ObservableCollection<Unidade> Unidades { get; } = new();
    public ObservableCollection<Funcionario> Funcionarios { get; } = new();
    public ObservableCollection<AvaliacaoItem> UltimasAvaliacoes { get; } = new();

    [ObservableProperty] private string csatHeadline = "—";
    [ObservableProperty] private string npsHeadline = "—";
    [ObservableProperty] private string respostasHeadline = "—";
    [ObservableProperty] private string detratoresHeadline = "—";

    [ObservableProperty] private Unidade? unidadeSelecionada;
    [ObservableProperty] private Funcionario? funcionarioSelecionado;
    [ObservableProperty] private string? filtroComentarios;
    [ObservableProperty] private bool isBusy;

    private List<Avaliacao> _cacheAll = new();
    private DateTime? _deUtc;
    private DateTime? _ateUtc;

    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private int _loadRuns = 0;

    public AdminViewModel(SupabaseService supa) => _supa = supa;

    // ===== Helpers (apenas com os campos do seu model) =====
    private static int Clamp15(int v) => Math.Min(5, Math.Max(1, v));
    private static double GetNota15(Avaliacao a) => (a.Nota >= 1 && a.Nota <= 5) ? a.Nota : 0;
    private static int GetEmojiScore(Avaliacao a)
    {
        if (a.EmojiScore is >= 1 and <= 5) return a.EmojiScore;
        var n = GetNota15(a);
        return n > 0 ? Clamp15((int)Math.Round(n)) : 3; // neutro
    }

    // Helpers de coleção (repintar com segurança)
    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> data)
    {
        target.Clear();
        foreach (var item in data) target.Add(item);
    }

    private async Task RunLockedAsync(Func<Task> action)
    {
        await _gate.WaitAsync();
        try { await action(); }
        finally { _gate.Release(); }
    }

    // ===== Reações a filtros =====
    partial void OnUnidadeSelecionadaChanged(Unidade? value)
    {
        // roda serializado para evitar colisão com Load/Refresh
        _ = RunLockedAsync(async () =>
        {
            await RecarregarFuncionariosDaUnidadeAsync();
            RecalcularExibicao();
        });
    }

    partial void OnFuncionarioSelecionadoChanged(Funcionario? value)
        => _ = RunLockedAsync(async () => { RecalcularExibicao(); await Task.CompletedTask; });

    partial void OnFiltroComentariosChanged(string? value) => AplicarFiltroComentariosTexto();

    // ===== Load / Refresh =====
    [RelayCommand]
    public async Task LoadAsync()
    {
        // Se já estiver rodando, sai
        if (!await _loadGate.WaitAsync(0))
            return;

        var runId = Interlocked.Increment(ref _loadRuns);
        try
        {
            IsBusy = true;

            // (LOG para descobrir o chamador)
            Debug.WriteLine($"[VM] LoadAsync RUN #{runId} started by:\n{new StackTrace(true)}");

            await _supa.InitializeAsync();

            Unidades.Clear();
            var unidades = await _supa.GetUnidadesAsync();
            foreach (var u in unidades)
                Unidades.Add(new Unidade(u.IdUnidade.ToString(), u.Nome));

            await CarregarTodosFuncionariosAsync();

            _deUtc = null; _ateUtc = null;

            _cacheAll = await _supa.GetAvaliacoesAsync(null, null, _deUtc, _ateUtc, limit: 5000);

            RecalcularExibicao();
        }
        finally
        {
            IsBusy = false;
            _loadGate.Release();
        }
    }


    private async Task CarregarTodosFuncionariosAsync()
    {
        var acum = new List<Funcionario>();
        foreach (var u in Unidades)
        {
            if (!int.TryParse(u.Id, out var idU)) continue;
            var funs = await _supa.GetFuncionariosAsync(idU);
            acum.AddRange(funs.Select(f => new Funcionario(f.Id.ToString(), f.Name)));
        }
        ReplaceCollection(Funcionarios, acum.GroupBy(x => x.Id).Select(g => g.First()));
    }

    private async Task RecarregarFuncionariosDaUnidadeAsync()
    {
        if (UnidadeSelecionada is null)
        {
            await CarregarTodosFuncionariosAsync();
            return;
        }

        if (!int.TryParse(UnidadeSelecionada.Id, out var idUnidade)) return;

        var funs = await _supa.GetFuncionariosAsync(idUnidade);
        ReplaceCollection(Funcionarios, funs.Select(f => new Funcionario(f.Id.ToString(), f.Name)));

        if (FuncionarioSelecionado is not null &&
            !Funcionarios.Any(x => x.Id == FuncionarioSelecionado.Id))
            FuncionarioSelecionado = null;
    }

    // ===== Botões =====
    [RelayCommand]
    private async Task FiltrarHoje()
    {
        await RunLockedAsync(async () =>
        {
            try
            {
                IsBusy = true;
                var hojeLocal = DateTime.Now.Date;
                _deUtc = DateTime.SpecifyKind(hojeLocal, DateTimeKind.Local).ToUniversalTime();
                _ateUtc = DateTime.SpecifyKind(hojeLocal.AddDays(1).AddTicks(-1), DateTimeKind.Local).ToUniversalTime();

                _cacheAll = await _supa.GetAvaliacoesAsync(null, null, _deUtc, _ateUtc, limit: 5000);
                RecalcularExibicao();
            }
            finally { IsBusy = false; }
        });
    }

    [RelayCommand]
    private async Task Exportar()
    {
        var linhas = new List<string> { "Unidade;CSAT" };
        linhas.AddRange(CsatPorUnidade.Select(u => $"{u.Nome};{u.Csat:0}"));
        var csv = string.Join(Environment.NewLine, linhas);
        await Task.CompletedTask; // TODO: salvar/compartilhar
    }

    // ===== Core =====
    private void RecalcularExibicao()
    {
        var baseAll = _cacheAll;

        int? uniId = (UnidadeSelecionada != null && int.TryParse(UnidadeSelecionada.Id, out var idU)) ? idU : null;
        int? funcId = (FuncionarioSelecionado != null && int.TryParse(FuncionarioSelecionado.Id, out var idF)) ? idF : null;

        // CSAT por unidade
        if (uniId is null)
        {
            var porUnidade = baseAll
                .GroupBy(a => a.IdUnidade)
                .Select(g =>
                {
                    var notas = g.Select(GetNota15).Where(n => n > 0).ToList();
                    var media = notas.Any() ? notas.Average() * 20.0 : 0;
                    return new CsatUnidade(
                        Unidades.FirstOrDefault(u => u.Id == g.Key.ToString())?.Nome ?? $"Unidade {g.Key}",
                        Math.Round(media, 0),
                        media switch { < 40 => "#E53935", < 60 => "#FFB300", < 80 => "#26A69A", _ => "#66BB6A" }
                    );
                })
                .OrderBy(x => x.Nome)
                .ToList();

            ReplaceCollection(CsatPorUnidade, porUnidade);
        }
        else
        {
            var daUnidade = baseAll.Where(a => a.IdUnidade == uniId);
            var notas = daUnidade.Select(GetNota15).Where(n => n > 0).ToList();
            var media = notas.Any() ? notas.Average() * 20.0 : 0;
            var nome = Unidades.FirstOrDefault(u => u.Id == uniId.ToString())?.Nome ?? $"Unidade {uniId}";
            var item = new CsatUnidade(nome, Math.Round(media, 0),
                media switch { < 40 => "#E53935", < 60 => "#FFB300", < 80 => "#26A69A", _ => "#66BB6A" });
            ReplaceCollection(CsatPorUnidade, new[] { item });
        }

        // Base para emojis/comentários
        IEnumerable<Avaliacao> baseDetalhe = baseAll;
        if (uniId is not null) baseDetalhe = baseDetalhe.Where(a => a.IdUnidade == uniId);
        if (funcId is not null) baseDetalhe = baseDetalhe.Where(a => a.IdFuncionario == funcId);
        var lista = baseDetalhe.ToList();

        // Distribuição por emoji
        var mapaEmoji = new[] { "😟", "😐", "🙂", "😊", "😁" };
        var cores = new[] { "#EF4444", "#F59E0B", "#FACC15", "#86EFAC", "#15803D" };
        var counts = new int[6]; // 1..5
        foreach (var a in lista) counts[GetEmojiScore(a)]++;

        var buckets = Enumerable.Range(1, 5)
            .Select(i => new EmojiBucket(mapaEmoji[i - 1], counts[i], cores[i - 1]))
            .ToList();
        ReplaceCollection(DistribEmoji, buckets);

        // Últimos comentários (somente com texto)
        var ultimos = lista
            .Where(x => !string.IsNullOrWhiteSpace(x.Comentario))
            .OrderByDescending(x => x.CriadoEm)
            .Take(200)
            .Select(a =>
            {
                var nomeUnidade = Unidades.FirstOrDefault(u => u.Id == a.IdUnidade.ToString())?.Nome
                                  ?? $"Unidade {a.IdUnidade}";
                var score = GetEmojiScore(a);
                return new AvaliacaoItem
                {
                    Emoji = mapaEmoji[Math.Clamp(score - 1, 0, 4)],
                    Comentario = a.Comentario!,
                    Data = a.CriadoEm.LocalDateTime,
                    Unidade = nomeUnidade
                };
            })
            .ToList();
        ReplaceCollection(UltimasAvaliacoes, ultimos);

        AtualizarCards(lista);
    }

    private void AtualizarCards(List<Avaliacao> baseFiltrada)
    {
        var total = baseFiltrada.Count;
        var notas = baseFiltrada.Select(GetNota15).Where(n => n > 0).ToList();
        var csat = notas.Any() ? notas.Average() * 20.0 : 0;
        CsatHeadline = Math.Round(csat, 0).ToString("0");

        if (total == 0)
        {
            NpsHeadline = "0";
            RespostasHeadline = "0";
            DetratoresHeadline = "0%";
            return;
        }

        var detratores = baseFiltrada.Count(a => GetEmojiScore(a) <= 2);
        var promotores = baseFiltrada.Count(a => GetEmojiScore(a) >= 5);
        var nps = ((double)promotores / total - (double)detratores / total) * 100.0;

        NpsHeadline = Math.Round(nps, 0).ToString("0");
        RespostasHeadline = total.ToString();
        DetratoresHeadline = $"{Math.Round((double)detratores / total * 100.0, 0):0}%";
    }

    // Filtro de texto client-side
    private void AplicarFiltroComentariosTexto()
    {
        if (string.IsNullOrWhiteSpace(FiltroComentarios)) return;

        var termo = FiltroComentarios.Trim();
        var filtrados = UltimasAvaliacoes
            .Where(a => (a.Comentario?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false)
                     || (a.Unidade?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(a => a.Data)
            .ToList();

        ReplaceCollection(UltimasAvaliacoes, filtrados);
    }
}
