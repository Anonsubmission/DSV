::@echo off 

SET POIROT_ROOT=C:\CCP\boogie
SET my_dir=C:\CCP\teamproject\FacebookProofCode\OpenAuth
SET STUB=stub
SET file_name=bin\Debug\OpenAuth
::SET file_name=OpenAuth
SET model_name=OpenAuth
SET clean_name=progClean

C:
cd %my_dir%

if exist *.exe del *.exe  
if exist *.pdb del *.pdb
if exist *.bpl del *.bpl 
if exist corral_out_trace.txt del corral_out_trace.txt

copy %POIROT_ROOT%\poirot4.net\library\poirot_stubs.bpl

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /t:library /nowarn:108,436 /r:bin\Debug\DotNetOpenAuth.Core.dll;bin\Debug\DotNetOpenAuth.OpenId.dll;bin\Debug\DotNetOpenAuth.OpenId.RelyingParty.dll /debug /D:DEBUG stub.cs
 
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe FBOAuth.csproj

call %POIROT_ROOT%\Poirot4.NET\BCT\BytecodeTranslator.exe /e:1 /lib:Stubs /ib /whole /heap:splitFields %file_name%.exe %STUB%.dll
call %POIROT_ROOT%\Corral\BctCleanup.exe %model_name%.bpl %clean_name%.bpl /main:PoirotMain.Main /include:poirot_stubs.bpl 
call %POIROT_ROOT%\Corral\corral.exe %clean_name%.bpl /recursionBound:2 /k:1 /main:PoirotMain.Main /tryCTrace


if exist corral_out_trace.txt %POIROT_ROOT%\ConcurrencyExplorer.exe corral_out_trace.txt


:end


  