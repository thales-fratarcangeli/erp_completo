namespace ErpLoja.Forms;

/// <summary>Painel gerencial (indicadores e gráficos), aberto pelo menu Gestão.</summary>
public class DashboardForm : Form
{
    public DashboardForm()
    {
        Ui.Estilizar(this, "Painel gerencial", 1300, 820, nameof(DashboardForm));
        WindowState = FormWindowState.Maximized;
        Controls.Add(new DashboardPainel { Dock = DockStyle.Fill });
    }
}
