# Gera a logo do ERP Loja (PNG 512px + ICO multi-tamanho) em ErpLoja\Recursos.
# Uso: powershell -ExecutionPolicy Bypass -File ferramentas\gerar-logo.ps1
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
$destino = Join-Path $PSScriptRoot '..\ErpLoja\Recursos'
New-Item -ItemType Directory -Force $destino | Out-Null

function Desenhar([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    # fundo: quadrado arredondado em degradê azul
    $r = $s * 0.22; $d = $r * 2; $m = [Math]::Max(0.5, $s * 0.02); $w = $s - 2 * $m
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($m, $m, $d, $d, 180, 90)
    $path.AddArc($m + $w - $d, $m, $d, $d, 270, 90)
    $path.AddArc($m + $w - $d, $m + $w - $d, $d, $d, 0, 90)
    $path.AddArc($m, $m + $w - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $rect = New-Object System.Drawing.RectangleF 0, 0, $s, $s
    $fundo = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.ColorTranslator]::FromHtml('#3987e5')), ([System.Drawing.ColorTranslator]::FromHtml('#184f95')), 90
    $g.FillPath($fundo, $path)

    $branco = [System.Drawing.Brushes]::White
    # toldo da loja: faixa com 4 "ondas" embaixo
    $x0 = $s * 0.17; $x1 = $s * 0.83; $yTopo = $s * 0.19; $yBase = $s * 0.33
    $g.FillRectangle($branco, $x0, $yTopo, $x1 - $x0, $yBase - $yTopo + $s * 0.015)
    $onda = ($x1 - $x0) / 4
    for ($i = 0; $i -lt 4; $i++) {
        $g.FillPie($branco, $x0 + $i * $onda, $yBase - $onda / 2, $onda, $onda, 0, 180)
    }

    # três barras crescentes (gestão / resultados)
    $base = $s * 0.81; $larg = $s * 0.14
    $alturas = @(0.15, 0.24, 0.33)
    $xs = @(0.215, 0.43, 0.645)
    for ($i = 0; $i -lt 3; $i++) {
        $h = $s * $alturas[$i]; $x = $s * $xs[$i]; $rr = [Math]::Max(1, $larg * 0.3)
        $b = New-Object System.Drawing.Drawing2D.GraphicsPath
        $b.AddLine($x, $base, $x, $base - $h + $rr)
        $b.AddArc($x, $base - $h, $rr * 2, $rr * 2, 180, 90)
        $b.AddArc($x + $larg - $rr * 2, $base - $h, $rr * 2, $rr * 2, 270, 90)
        $b.AddLine($x + $larg, $base - $h + $rr, $x + $larg, $base)
        $b.CloseFigure()
        $g.FillPath($branco, $b)
    }
    $g.Dispose()
    return $bmp
}

# PNG grande (usado na tela de login e no README)
$png = Desenhar 512
$png.Save((Join-Path $destino 'logo.png'), [System.Drawing.Imaging.ImageFormat]::Png)

# ICO com várias resoluções (entradas em PNG)
$tamanhos = 16, 24, 32, 48, 64, 128, 256
$imagens = foreach ($t in $tamanhos) {
    $ms = New-Object System.IO.MemoryStream
    (Desenhar $t).Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    , $ms.ToArray()
}
$saida = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter $saida
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$tamanhos.Count)
$offset = 6 + 16 * $tamanhos.Count
for ($i = 0; $i -lt $tamanhos.Count; $i++) {
    $t = $tamanhos[$i]; $dados = $imagens[$i]
    $bw.Write([byte]($t % 256)); $bw.Write([byte]($t % 256)); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32); $bw.Write([UInt32]$dados.Length); $bw.Write([UInt32]$offset)
    $offset += $dados.Length
}
foreach ($dados in $imagens) { $bw.Write($dados) }
[System.IO.File]::WriteAllBytes((Join-Path $destino 'logo.ico'), $saida.ToArray())

# prévia ampliada dos tamanhos pequenos para conferência
$previa = New-Object System.Drawing.Bitmap 760, 300
$gp = [System.Drawing.Graphics]::FromImage($previa)
$gp.Clear([System.Drawing.Color]::White)
$gp.InterpolationMode = 'NearestNeighbor'
$x = 10
foreach ($t in 16, 32, 48) { $gp.DrawImage((Desenhar $t), $x, 10, $t * 4, $t * 4); $x += $t * 4 + 20 }
$gp.DrawImage($png, $x, 10, 200, 200)
$gp.Dispose()
$previa.Save((Join-Path $env:TEMP 'erp_logo_previa.png'), [System.Drawing.Imaging.ImageFormat]::Png)
"ok"
