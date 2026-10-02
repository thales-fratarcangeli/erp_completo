# Gera a logo do sistema (PNG 512px + ICO multi-tamanho) a partir da arte original.
# Entrada: ErpLoja\Recursos\logo-original.png (arte "CE" fornecida pela empresa).
# - recorta as margens brancas, torna o fundo branco transparente (com borda suavizada) e centraliza em quadrado;
# - gera logo.png (512 px) e logo.ico (16, 24, 32, 48, 64, 128 e 256 px).
# Uso: powershell -ExecutionPolicy Bypass -File ferramentas\gerar-logo.ps1
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
$recursos = Join-Path $PSScriptRoot '..\ErpLoja\Recursos'
$origem = Join-Path $recursos 'logo-original.png'

$src = [System.Drawing.Bitmap]::FromFile((Resolve-Path $origem))
$w = $src.Width; $h = $src.Height

# 1) fundo branco -> transparente (alfa proporcional à "distância do branco", para bordas suaves)
$transp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$rect = New-Object System.Drawing.Rectangle 0, 0, $w, $h
$dSrc = $src.LockBits($rect, 'ReadOnly', ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb))
$dDst = $transp.LockBits($rect, 'WriteOnly', ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb))
$bytes = New-Object byte[] ($dSrc.Stride * $h)
[System.Runtime.InteropServices.Marshal]::Copy($dSrc.Scan0, $bytes, 0, $bytes.Length)
$minX = $w; $minY = $h; $maxX = 0; $maxY = 0
for ($y = 0; $y -lt $h; $y++) {
    $linha = $y * $dSrc.Stride
    for ($x = 0; $x -lt $w; $x++) {
        $i = $linha + $x * 4
        $b = $bytes[$i]; $g = $bytes[$i + 1]; $r = $bytes[$i + 2]
        $min = [Math]::Min($r, [Math]::Min($g, $b))
        # quanto mais perto do branco, mais transparente
        $alfa = [Math]::Min(255, [Math]::Max(0, (255 - $min) * 2))
        if ($alfa -lt 12) { $alfa = 0 }
        $bytes[$i + 3] = [byte]$alfa
        if ($alfa -gt 0) {
            # recupera a cor "sem o branco" para não deixar halo claro
            $bytes[$i] = 0xB8; $bytes[$i + 1] = 0x65; $bytes[$i + 2] = 0x15   # B, G, R = azul #1565B8
            if ($x -lt $minX) { $minX = $x }; if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }; if ($y -gt $maxY) { $maxY = $y }
        }
    }
}
[System.Runtime.InteropServices.Marshal]::Copy($bytes, 0, $dDst.Scan0, $bytes.Length)
$src.UnlockBits($dSrc); $transp.UnlockBits($dDst); $src.Dispose()

# 2) recorte + quadrado com margem de 6%
$larg = $maxX - $minX + 1; $alt = $maxY - $minY + 1
$lado = [int]([Math]::Max($larg, $alt) * 1.12)
$quadrado = New-Object System.Drawing.Bitmap $lado, $lado, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gq = [System.Drawing.Graphics]::FromImage($quadrado)
$gq.Clear([System.Drawing.Color]::Transparent)
$gq.DrawImage($transp, (New-Object System.Drawing.Rectangle ([int](($lado - $larg) / 2)), ([int](($lado - $alt) / 2)), $larg, $alt),
    (New-Object System.Drawing.Rectangle $minX, $minY, $larg, $alt), 'Pixel')
$gq.Dispose(); $transp.Dispose()

function Redimensionar([int]$t) {
    $bmp = New-Object System.Drawing.Bitmap $t, $t, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = 'HighQualityBicubic'; $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($quadrado, 0, 0, $t, $t)
    $g.Dispose()
    return $bmp
}

(Redimensionar 512).Save((Join-Path $recursos 'logo.png'), [System.Drawing.Imaging.ImageFormat]::Png)

$tamanhos = 16, 24, 32, 48, 64, 128, 256
$imagens = foreach ($t in $tamanhos) {
    $ms = New-Object System.IO.MemoryStream
    (Redimensionar $t).Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
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
[System.IO.File]::WriteAllBytes((Join-Path $recursos 'logo.ico'), $saida.ToArray())

# prévia: tamanhos pequenos ampliados, em fundo claro e escuro
$previa = New-Object System.Drawing.Bitmap 900, 260
$gp = [System.Drawing.Graphics]::FromImage($previa)
$gp.Clear([System.Drawing.Color]::White)
$gp.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(17, 24, 39))), 450, 0, 450, 260)
$gp.InterpolationMode = 'NearestNeighbor'
foreach ($base in 0, 450) {
    $x = $base + 10
    foreach ($t in 16, 32, 48) { $gp.DrawImage((Redimensionar $t), $x, 10, $t * 3, $t * 3); $x += $t * 3 + 14 }
    $gp.DrawImage((Redimensionar 128), $x, 10, 128, 128)
}
$gp.Dispose()
$previa.Save((Join-Path $env:TEMP 'erp_logo_previa.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$quadrado.Dispose()
"ok"
