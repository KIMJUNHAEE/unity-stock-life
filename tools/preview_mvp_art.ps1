$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path -Parent $PSScriptRoot
$jobs = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'mvp_art_jobs.json') -Raw | ConvertFrom-Json
$jobs = @($jobs | Where-Object { $_.active -ne $false })
$fontCollection = [System.Drawing.Text.PrivateFontCollection]::new()
$fontCollection.AddFontFile((Join-Path $projectRoot 'Assets/Art/Fonts/GyeonggiTitle/GyeonggiTitle-Medium.ttf'))
$font = [System.Drawing.Font]::new($fontCollection.Families[0], 16)
$brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(47,62,82))
function Write-ContactSheet($entries, $name, $columns, $cellWidth, $cellHeight) {
    $rows = [int][Math]::Ceiling($entries.Count / [double]$columns)
    $canvas = [System.Drawing.Bitmap]::new([int]($columns*$cellWidth), [int]($rows*$cellHeight))
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    $graphics.Clear([System.Drawing.Color]::FromArgb(246,248,252))
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    for ($i=0; $i -lt $entries.Count; $i++) {
        $entry=$entries[$i]
        $picture=[System.Drawing.Image]::FromFile((Join-Path $projectRoot $entry.path))
        $scale=[Math]::Min(($cellWidth-24)/$picture.Width, ($cellHeight-52)/$picture.Height)
        $width=[int]($picture.Width*$scale); $height=[int]($picture.Height*$scale)
        $left=($i % $columns)*$cellWidth; $top=[int][Math]::Floor($i/[double]$columns)*$cellHeight
        $x=[int]($left+($cellWidth-$width)/2); $y=[int]($top+8+($cellHeight-52-$height)/2)
        $graphics.DrawImage($picture,$x,$y,$width,$height)
        $graphics.DrawString([string]$entry.key,$font,$brush,[single]($left+12),[single]($top+$cellHeight-32))
        $picture.Dispose()
    }
    $canvas.Save((Join-Path $projectRoot ('docs/previews/'+$name+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $canvas.Dispose()
}
$characters=@($jobs | Where-Object { $_.kind -in @('seoha','customer') })
Write-ContactSheet $characters 'mvp_characters_contact_v001' 5 360 520
$objects=@($jobs | Where-Object { $_.kind -in @('furniture','product','prop') })
foreach ($app in @('stock','browser','finance','album','settings')) {
    $objects += [pscustomobject]@{key=('icon_'+$app);path=('Assets/Art/UI/Icons/ui_icon_'+$app+'_v001.png')}
}
Write-ContactSheet $objects 'mvp_objects_contact_v001' 6 300 280
$backgrounds=@($jobs | Where-Object { $_.kind -in @('background','wallpaper') })
Write-ContactSheet $backgrounds 'mvp_backgrounds_contact_v001' 2 800 490
$font.Dispose(); $brush.Dispose(); $fontCollection.Dispose()
Write-Output 'Created QA contact sheets; source PNGs unchanged.'
