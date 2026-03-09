Add-Type -AssemblyName System.Drawing

$iconPath = "C:\Projets\SmartGPON\SmartGPON_icon.ico"
$shortcutPath = "$env:USERPROFILE\Desktop\SmartGPON.lnk"
$targetPath = "C:\Projets\SmartGPON\bin\Debug\net8.0\SmartGPON.exe"
$workingDir = "C:\Projets\SmartGPON"

Write-Host "Création de l'icône G..."
# 1. Créer une image 256x256
$bmp = [System.Drawing.Bitmap]::new(256, 256)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Lettre G en style 3D/Bleu dynamically generated
$textBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(33, 86, 243)) # Bleu 
$font = [System.Drawing.Font]::new("Arial", 180, [System.Drawing.FontStyle]::Bold)
$format = [System.Drawing.StringFormat]::new()
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center
$rect = [System.Drawing.RectangleF]::new(0, 0, 256, 256)

# Ajouter l'ombre
$shadowBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(100, 0, 0, 0))
$shadowRect = [System.Drawing.RectangleF]::new(8, 8, 256, 256)
$g.DrawString("G", $font, $shadowBrush, $shadowRect, $format)

# Ajouter le texte
$g.DrawString("G", $font, $textBrush, $rect, $format)

# Sauvegarder en tant que fichier .ICO de manière robuste (PNG encodé en ICO)
$ms = [System.IO.MemoryStream]::new()
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $ms.ToArray()

$fs = [System.IO.FileStream]::new($iconPath, [System.IO.FileMode]::Create)
$bw = [System.IO.BinaryWriter]::new($fs)
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]1)
$bw.Write([byte]0)
$bw.Write([byte]0)
$bw.Write([byte]0)
$bw.Write([byte]0)
$bw.Write([uint16]1)
$bw.Write([uint16]32)
$bw.Write([uint32]$pngBytes.Length)
$bw.Write([uint32]22)
$bw.Write($pngBytes)
$bw.Close()
$fs.Close()
$ms.Close()
$g.Dispose()
$bmp.Dispose()

Write-Host "Icone crée avec succès à '$iconPath'"

# 2. Créer le raccourci sur le bureau
Write-Host "Création du raccourci sur le bureau..."
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($shortcutPath)
$Shortcut.TargetPath = $targetPath
$Shortcut.WorkingDirectory = $workingDir
$Shortcut.IconLocation = $iconPath
$Shortcut.Description = "Lancer l'application SmartGPON"
$Shortcut.WindowStyle = 1
$Shortcut.Save()

Write-Host "Le raccourci 'SmartGPON' a été créé avec succès sur le bureau !"
