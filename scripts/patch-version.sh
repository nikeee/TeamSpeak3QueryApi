#!/usr/bin/env bash

CSPROJ_FILE=src/TeamSpeak3QueryApi/TeamSpeak3QueryApi.csproj

function get_xml_version() {
    grep -oiP "\<PackageVersion\>(.*)\<\/PackageVersion\>.*" "${CSPROJ_FILE}" | grep -oP "\d+\.\d+\.\d+"
}

function git_describe() {
    git describe --tags --long
}

function get_git_commit_id() {
    git_describe | grep -oP "\-[0-9a-zA-Z]+$" | grep -oP "[0-9a-zA-Z]+$"
}
function get_git_commits_since_last_tag() {
    git_describe | grep -oP "\-\d+-" | grep -oP "\d+"
}


XML_VERSION=$(get_xml_version)
COMMIT_ID=$(get_git_commit_id)
COMMITS_SINCE_LAST_TAG=$(get_git_commits_since_last_tag)
RELEASE_TYPE="beta"

# Utility: https://jubianchi.github.io/version
# Sample version:
# 1.2.3-beta.3+ab3fafb
VERSION="${XML_VERSION}-${RELEASE_TYPE}.${COMMITS_SINCE_LAST_TAG}+${COMMIT_ID}"

echo "Using patched version: ${VERSION}"
echo "Patching file.."

sed -i "s/${XML_VERSION}/${VERSION}/g" "${CSPROJ_FILE}"
echo "File patched!"
