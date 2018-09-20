#!/usr/bin/env bash

echo "Packing..."
dotnet pack -c Release src/TeamSpeak3QueryApi/TeamSpeak3QueryApi.csproj

echo "Pushing..."
dotnet nuget push -k $NUGET_KEY -s https://api.nuget.org/v3/index.json src/TeamSpeak3QueryApi/bin/Release/*.nupkg
echo "Pushed!"
