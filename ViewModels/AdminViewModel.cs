using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalia_.ViewModels
{
    public partial class AdminViewModel : ObservableObject
    {
        // ====== MODELOS ======
        public record CsatUnidade(string Nome, double Csat, string Cor);
        public record EmojiBucket(string Emoji, int Quantidade, string Cor);
        public record Unidade(string Id, string Nome);                  // Picker de unidade
        public record Funcionario(string Id, string Nome);              // Picker de funcionário

        // ====== COLEÇÕES (BINDINGS) ======
        public ObservableCollection<CsatUnidade> CsatPorUnidade { get; } = new();
        public ObservableCollection<EmojiBucket> DistribEmoji { get; } = new();

        public ObservableCollection<Unidade> Unidades { get; } = new();
        public ObservableCollection<Funcionario> Funcionarios { get; } = new();

        // ====== FILTROS SELECIONADOS ======
        [ObservableProperty] private Unidade? unidadeSelecionada;
        partial void OnUnidadeSelecionadaChanged(Unidade? value)
        {
            AtualizarDistribuicaoPorEmoji();
        }

        [ObservableProperty] private Funcionario? funcionarioSelecionado;
        partial void OnFuncionarioSelecionadoChanged(Funcionario? value)
        {
            AtualizarDistribuicaoPorEmoji();
        }

        // ====== CTOR ======
        public AdminViewModel()
        {
            // mocks iniciais (troque por chamadas ao serviço/api)
            Unidades.Add(new Unidade("A", "Unidade A"));
            Unidades.Add(new Unidade("B", "Unidade B"));
            Unidades.Add(new Unidade("C", "Unidade C"));
            Unidades.Add(new Unidade("D", "Unidade D"));
            Unidades.Add(new Unidade("E", "Unidade E"));


            Funcionarios.Add(new Funcionario("1", "Ana"));
            Funcionarios.Add(new Funcionario("2", "Bruno"));
            Funcionarios.Add(new Funcionario("3", "Carla"));

            // CSAT por unidade (tons azul→verde, variando por barra)
            CsatPorUnidade.Clear();
            CsatPorUnidade.Add(new CsatUnidade("Unidade A", 42, "#1E88E5")); // azul
            CsatPorUnidade.Add(new CsatUnidade("Unidade A", 50, "#2196F3")); // azul-claro
            CsatPorUnidade.Add(new CsatUnidade("Unidade B", 72, "#26A69A")); // teal
            CsatPorUnidade.Add(new CsatUnidade("Unidade C", 82, "#66BB6A")); // verde
            CsatPorUnidade.Add(new CsatUnidade("Unidade C", 80, "#9CCC65")); // verde-claro

            // seleção padrão e carga inicial do gráfico 2
            UnidadeSelecionada = Unidades.FirstOrDefault();
            AtualizarDistribuicaoPorEmoji();
        }

        // ====== COMANDOS ======
        [RelayCommand]
        private void FiltrarHoje()
        {
            // Exemplo: recarregar dados do dia atual.
            // Aqui você chamaria sua API/Service passando um intervalo "Hoje".
            // Para demo, só vou reajustar leve os valores.
            var rand = new Random();

            for (int i = 0; i < CsatPorUnidade.Count; i++)
            {
                var u = CsatPorUnidade[i];
                var novo = Math.Clamp(u.Csat + rand.Next(-4, 5), 0, 100);
                CsatPorUnidade[i] = u with { Csat = novo };
            }

            AtualizarDistribuicaoPorEmoji();
        }

        [RelayCommand]
        private async Task Exportar()
        {
            // Implemente conforme sua necessidade (CSV/PDF/Excel).
            // Exemplo simples: gerar CSV em memória (stub).
            var linhas = new List<string>
        {
            "Unidade;CSAT",
        };
            linhas.AddRange(CsatPorUnidade.Select(u => $"{u.Nome};{u.Csat:0}"));

            var csv = string.Join(Environment.NewLine, linhas);
            // TODO: salvar/compartilhar csv
            await Task.CompletedTask;
        }

        // ====== LÓGICA ======
        private void AtualizarDistribuicaoPorEmoji()
        {
            // Este método deve consultar seu backend considerando:
            // UnidadeSelecionada e FuncionarioSelecionado.
            // Abaixo, só gero números mock com um "shape" parecido com o print.

            DistribEmoji.Clear();

            var seed = (UnidadeSelecionada?.Id ?? "X") + "|" + (FuncionarioSelecionado?.Id ?? "0");
            var hash = seed.GetHashCode();
            var rnd = new Random(hash);

            // distribuição 5 barras (😡, 😟, 🙂, 😊, 😁) – tendendo ao positivo
            var v1 = rnd.Next(3, 10);
            var v2 = rnd.Next(8, 18);
            var v3 = rnd.Next(14, 28);
            var v4 = rnd.Next(22, 38);
            var v5 = rnd.Next(32, 55);

            // Distribuição por emoji (vermelho→laranja→amarelo→verde-claro→verde-escuro)
            DistribEmoji.Clear();
            DistribEmoji.Add(new EmojiBucket("😡", v1, "#EF4444")); // vermelho
            DistribEmoji.Add(new EmojiBucket("😟", v2, "#F59E0B")); // laranja
            DistribEmoji.Add(new EmojiBucket("🙂", v3, "#FACC15")); // amarelo
            DistribEmoji.Add(new EmojiBucket("😊", v4, "#86EFAC")); // verde-claro
            DistribEmoji.Add(new EmojiBucket("😁", v5, "#15803D")); // verde-escuro
        }
    }
}
