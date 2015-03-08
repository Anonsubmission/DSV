::@echo off 
::@echo off 

SET POIROT_ROOT=C:\poirot
SET my_dir=C:\CCP\teamprojects\NopCommerce-PayPal-MTP\NopCommerce
SET STUB=stub
SET file_name=bin\Debug\NopCommerce
::SET file_name=NopCommerce
SET model_name=NopCommerce
SET clean_name=progClean

C:
cd %my_dir%

if exist *.exe del *.exe  
if exist *.pdb del *.pdb
if exist *.bpl del *.bpl 
if exist corral_out_trace.txt del corral_out_trace.txt


copy %POIROT_ROOT%\poirot4.net\library\poirot_stubs.bpl

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /t:library /nowarn:108,436 /r:bin\Debug\Nop.BusinessLogic.dll /debug /D:DEBUG %STUB%.cs
 
::C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe NopCommerce.csproj

call %POIROT_ROOT%\Poirot4.NET\BCT\BytecodeTranslator.exe /e:1 /lib:Stub /ib /whole /heap:splitFields %file_name%.exe %STUB%.dll

call %POIROT_ROOT%\Corral\BctCleanup.exe %model_name%.bpl %clean_name%.bpl /main:PoirotMain.Main /include:poirot_stubs.bpl 

call %POIROT_ROOT%\Corral\corral.exe %clean_name%.bpl /recursionBound:2 /k:1 /main:PoirotMain.Main /tryCTrace /trackAllVars /z3opt:MBQI=true

if exist corral_out_trace.txt %POIROT_ROOT%\ConcurrencyExplorer.exe corral_out_trace.txt

:end


  