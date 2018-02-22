
md "compiled\"
md "compiled\Config"
md "compiled\Log"

del "compiled\*.*" /f/q
del "compiled\Config\*.*" /f/q
del "compiled\Log\*.*" /f/q

set publish_path=%cd%\compiled
@echo publish_path: %publish_path%

"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" /v:quiet /p:Configuration=Release;Platform="AnyCPU";DeployOnBuild=true;OutputPath="%publish_path%" "Enginesoft.SyncS3.Service\Enginesoft.SyncS3.Service.csproj"

del %publish_path%\Config\main.json

