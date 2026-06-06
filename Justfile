set windows-shell := ["pwsh.exe", "-c"]

default:
  just --list

build:
  dotnet build Valo.App

run:
  dotnet run Valo.App

publish rid:
  dotnet publish -p:PublishProfile=Release -r {{rid}} Valo.App
