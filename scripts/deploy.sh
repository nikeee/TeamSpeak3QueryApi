#!/usr/bin/env bash

dotnet pack -c Release src/TeamSpeak3QueryApi/TeamSpeak3QueryApi.csproj
dotnet nuget push -k $NUGET_KEY -s https://api.nuget.org/v3/index.json src/TeamSpeak3QueryApi/TeamSpeak3QueryApi/bin/Release/*.nuget
