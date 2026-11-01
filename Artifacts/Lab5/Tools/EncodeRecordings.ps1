param([string]$ValidationPath = '.unity-validation')
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $ValidationPath
& .\ffmpeg.exe -hide_banner -y -framerate 30 -i 'Logs/TeenFrames/frame-%04d.png' -i 'Artifacts/Lab5/Videos/powerups-and-restart.mp4' -map '0:v:0' -map '1:a:0' -vf "subtitles=Artifacts/Lab5/Videos/powerups-and-restart.mp4.vtt:force_style='FontName=Arial,FontSize=16,Outline=1,Shadow=0,MarginV=14'" -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p -c:a copy 'Artifacts/Lab5/Videos/gameplay-final.mp4'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& .\ffmpeg.exe -hide_banner -y -framerate 30 -i 'Logs/ArchitectureFrames/frame-%04d.png' -i 'Artifacts/Lab5/Videos/architecture-inspector.mp4' -map '0:v:0' -map '1:a:0' -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p -c:a copy 'Artifacts/Lab5/Videos/architecture-final.mp4'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& .\ffmpeg.exe -hide_banner -y -i 'Artifacts/Lab5/Videos/architecture-final.mp4' -i 'Artifacts/Lab5/Videos/gameplay-final.mp4' -filter_complex '[0:v]scale=1640:924:force_original_aspect_ratio=decrease,pad=1640:924:(ow-iw)/2:(oh-ih)/2,setsar=1[v0];[1:v]scale=1640:924:force_original_aspect_ratio=decrease,pad=1640:924:(ow-iw)/2:(oh-ih)/2,setsar=1[v1];[v0][0:a][v1][1:a]concat=n=2:v=1:a=1[v][a]' -map '[v]' -map '[a]' -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p -c:a aac -b:a 128k 'Artifacts/Lab5/Videos/teens-checkoff.mp4'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$audioCheck = Start-Process -NoNewWindow -Wait -PassThru -FilePath '.\ffmpeg.exe' -ArgumentList @('-hide_banner','-i','Artifacts/Lab5/Videos/teens-checkoff.mp4','-af','volumedetect','-f','null','NUL') -RedirectStandardError 'Logs/teen-media-verification.txt'
if ($audioCheck.ExitCode -ne 0) { exit $audioCheck.ExitCode }
$decodeCheck = Start-Process -NoNewWindow -Wait -PassThru -FilePath '.\ffmpeg.exe' -ArgumentList @('-v','error','-i','Artifacts/Lab5/Videos/teens-checkoff.mp4','-f','null','NUL') -RedirectStandardError 'Logs/teen-media-decode-errors.txt'
exit $decodeCheck.ExitCode
