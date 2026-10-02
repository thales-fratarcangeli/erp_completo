param([Parameter(Mandatory)][string]$Pasta)
# Teste de fumaça da INTERFACE do executável publicado (inclusive ofuscado), por UI Automation.
# Pré-requisito: a pasta contém ErpLoja.exe e um loja.db com dados (ex.: ErpLoja.exe --dados-demo <pasta>\loja.db)
# e o usuário admin (senha admin) com e-mail cadastrado (evita o aviso de e-mail no login).
# Usa uma pasta de preferências própria (ERPLOJA_PREFERENCIAS) para não alterar a configuração da máquina.
# Uso: powershell -ExecutionPolicy Bypass -File testes\interface\teste-interface.ps1 -Pasta <pasta>
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, Microsoft.VisualBasic
Add-Type -Namespace '' -Name Win32Clique -MemberDefinition '[DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);'
$ErrorActionPreference = 'Stop'
$AE = [System.Windows.Automation.AutomationElement]
$Escopo = [System.Windows.Automation.TreeScope]
$env:ERPLOJA_PREFERENCIAS = Join-Path $Pasta 'prefs'
$proc = Start-Process (Join-Path $Pasta 'ErpLoja.exe') -PassThru
$log = New-Object System.Collections.Generic.List[string]

function Janelas {
    # janelas de topo do processo + janelas "com dono" (no UI Automation ficam dentro da janela dona)
    $doProcesso = New-Object System.Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $proc.Id)
    $ehJanela = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::Window)
    $fila = New-Object System.Collections.Generic.List[object]
    try { foreach ($w in $AE::RootElement.FindAll($Escopo::Children, $doProcesso)) { $fila.Add($w) } } catch { }
    for ($i = 0; $i -lt $fila.Count -and $i -lt 60; $i++) {
        try { foreach ($filha in $fila[$i].FindAll($Escopo::Children, $ehJanela)) { $fila.Add($filha) } } catch { }
    }
    return , $fila
}

function Esperar([string]$nome, [int]$segundos = 25) {
    $fim = (Get-Date).AddSeconds($segundos)
    while ((Get-Date) -lt $fim) {
        foreach ($w in (Janelas)) {
            $n = $w.Current.Name
            if ($n -eq 'Atenção' -or $n -like '*.NET*') {
                $texto = ($w.FindAll($Escopo::Descendants, [System.Windows.Automation.Condition]::TrueCondition) | ForEach-Object { $_.Current.Name } | Where-Object { $_ }) -join ' | '
                throw "janela de erro '$n': $texto"
            }
            if ($n -like $nome) { return $w }
        }
        Start-Sleep -Milliseconds 300
    }
    throw "janela '$nome' não apareceu. Abertas: " + (((Janelas) | ForEach-Object { $_.Current.Name }) -join ', ')
}

function Localizar($janela, [string]$nome) {
    $ehBotao = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
    $limite = (Get-Date).AddSeconds(6)
    do {
        $b = $janela.FindAll($Escopo::Descendants, $ehBotao) | Where-Object { $_.Current.Name.Trim() -like $nome } | Select-Object -First 1
        if ($null -ne $b) { return $b }
        Start-Sleep -Milliseconds 300
    } while ((Get-Date) -lt $limite)
    throw "botão '$nome' não encontrado em '$($janela.Current.Name)'"
}

function Botao($janela, [string]$nome) {
    (Localizar $janela $nome).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}

# botões que abrem janela modal: clique assíncrono (BM_CLICK por PostMessage), senão a automação fica presa até o modal fechar
function BotaoModal($janela, [string]$nome) {
    $b = Localizar $janela $nome
    [void][Win32Clique]::PostMessage([IntPtr]$b.Current.NativeWindowHandle, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 300
}

function Fechar($janela) { $janela.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close(); Start-Sleep -Milliseconds 500 }

function Teclas($janela, [string]$teclas) {
    [Microsoft.VisualBasic.Interaction]::AppActivate($proc.Id)
    $janela.SetFocus()
    Start-Sleep -Milliseconds 400
    [System.Windows.Forms.SendKeys]::SendWait($teclas)
}

function Abriu($janela) { Start-Sleep -Milliseconds 900; $log.Add('abriu: ' + $janela.Current.Name) }

try {
    $login = Esperar 'ERP Loja - Acesso'
    $edits = $login.FindAll($Escopo::Descendants, (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)))
    $edits[0].GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('admin')
    $edits[1].GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('admin')
    Botao $login 'Entrar'
    $principal = Esperar 'ERP Loja - [B-Z]*'
    $log.Add('login ok: ' + $principal.Current.Name)

    # menu lateral
    Botao $principal 'PDV - frente de caixa*'
    $j = Esperar 'PDV - frente de caixa'; Abriu $j; Fechar $j

    # atalhos de teclado
    foreach ($caso in @(@('{F5}', 'Painel gerencial'), @('^n', 'Notas fiscais'), @('{F6}', 'Estoque'), @('{F7}', 'Financeiro*'))) {
        Teclas $principal $caso[0]
        $j = Esperar $caso[1]; Abriu $j; Fechar $j
    }

    # editor com grade ligada a classe (ligação por nome de propriedade)
    $log.Add('passo: menu Orçamentos')
    Botao $principal 'Orçamentos*'
    $orc = Esperar 'Orçamentos'
    $log.Add('passo: lista aberta, clicando Novo')
    BotaoModal $orc 'Novo'
    $log.Add('passo: Novo clicado')
    Start-Sleep 4; $proc.Refresh(); $log.Add('processo respondendo: ' + $proc.Responding)
    $editor = Esperar 'Orçamento (novo)'; Abriu $editor; Fechar $editor; Fechar $orc

    # localizador de telas + ajuda (F1)
    Teclas $principal '^k'
    $null = Esperar 'Localizar tela'
    Start-Sleep -Milliseconds 500
    [System.Windows.Forms.SendKeys]::SendWait('produtos')
    Start-Sleep -Milliseconds 500
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    $produtos = Esperar 'Produtos'; Abriu $produtos
    Teclas $produtos '{F1}'
    $ajuda = Esperar 'Ajuda — *'; Abriu $ajuda; Fechar $ajuda; Fechar $produtos

    foreach ($w in (Janelas)) { if ($w.Current.Name -eq 'Atenção') { throw 'janela de erro ao final' } }
    $log.Add('RESULTADO: OK')
}
catch {
    $log.Add('RESULTADO: FALHA - ' + $_.Exception.Message)
}
finally {
    if (-not $proc.HasExited) { $proc.Kill() }
}
$log
if ($log[-1] -ne 'RESULTADO: OK') { exit 1 }
