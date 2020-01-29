@echo off
..\..\..\..\..\tools\protobuf-net\protogen.exe -i:%1.proto -o:%1.proto.cs
pause
@echo on
