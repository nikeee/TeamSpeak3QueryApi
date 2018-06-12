#!/usr/bin/env bash

dotnet pack -c Release src/TeamSpeak3QueryApi/TeamSpeak3QueryApi.csproj
dotnet nuget push -k $NUGET_KEY src/TeamSpeak3QueryApi/TeamSpeak3QueryApi/bin/Release/*.nuget
