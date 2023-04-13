#!/bin/bash

cd STGP-Sharp/STGP-Sharp
dotnet build -c release

cd ../../BomberlandGp/BomberlandGp/BomberlandGp
dotnet build -c release
dotnet publish -c release
