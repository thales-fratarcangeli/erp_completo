using ErpLoja.Data;
using ErpLoja.UI;

namespace ErpLoja.Testes.Cenarios;

/// <summary>Utilitários dos cenários de cadastros e fiscal: operar campos do CrudForm pelo rótulo, gerar documentos válidos.</summary>
public static class CadastrosApoio
{
    /// <summary>Editor do CrudForm pelo rótulo exibido (sem o " *" de obrigatório). CheckBox: pelo próprio texto.</summary>
    public static Control Campo(Form f, string rotulo)
    {
        if (Usuario.Todos(f).OfType<CheckBox>().FirstOrDefault(c => c.Text == rotulo) is { } marcador)
            return marcador;
        var lbl = Usuario.Todos(f).OfType<Label>().FirstOrDefault(l => l.Text.TrimEnd(' ', '*') == rotulo)
                  ?? throw new InvalidOperationException($"Campo '{rotulo}' não encontrado em '{f.Text}'.");
        var pai = lbl.Parent!;
        return pai.Controls[pai.Controls.GetChildIndex(lbl) + 1];
    }

    public static void Preencher(this Usuario u, Form f, string rotulo, object? valor)
    {
        switch (Campo(f, rotulo))
        {
            case TextBox t: t.Focus(); t.Text = valor?.ToString() ?? ""; break;
            case CheckBox c: c.Checked = valor is true; break;
            case NumericUpDown n: n.Value = Convert.ToDecimal(valor); break;
            case ComboBox cb when valor is string s: cb.SelectedItem = s; break;
            case ComboBox cb: Ui.Selecionar(cb, valor); break;
        }
        Usuario.Esperar(50);
    }

    /// <summary>Seleciona na grade a linha do registro (como clicar nela).</summary>
    public static bool SelecionarRegistro(this Usuario u, Form f, long id)
    {
        var grid = Usuario.Achar<DataGridView>(f);
        foreach (DataGridViewRow r in grid.Rows)
            if (r.DataBoundItem is System.Data.DataRowView v && Convert.ToInt64(v["id"]) == id)
            {
                grid.ClearSelection();
                grid.CurrentCell = r.Cells.Cast<DataGridViewCell>().First(c => c.Visible);
                r.Selected = true;
                Usuario.Esperar();
                return true;
            }
        return false;
    }

    public static long Contar(string tabela, string where = "1 = 1", params object?[] args) =>
        Convert.ToInt64(Db.Scalar($"SELECT COUNT(*) FROM {tabela} WHERE {where}", args));

    /// <summary>CPF válido a partir de 9 dígitos.</summary>
    public static string Cpf(string nove)
    {
        int Dv(string b) { var s = b.Select((c, i) => (c - '0') * (b.Length + 1 - i)).Sum() % 11; return s < 2 ? 0 : 11 - s; }
        var d1 = Dv(nove);
        var d2 = Dv(nove + d1);
        return $"{nove[..3]}.{nove[3..6]}.{nove[6..9]}-{d1}{d2}";
    }

    /// <summary>EAN-13 válido a partir de 12 dígitos.</summary>
    public static string Ean13(string doze)
    {
        var soma = doze.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
        return doze + (10 - soma % 10) % 10;
    }

    public static string Erros(Usuario u) => string.Concat(u.Mensagens.Where(m => m.Tipo == "ERRO").Select(m => " | " + m.Texto));
}
