all: build

PROJECT_NAME=CoCoL
DEPENDS_CS=$(shell find . -type f -name *.cs | xargs echo)
VERSION=$(shell cat $(PROJECT_NAME).nuspec | grep "<version>" | tr -d "[A-z/<>] ")

build: $(PROJECT_NAME).sln $(DEPENDS_CS)
	nuget restore $(PROJECT_NAME).sln
	dotnet restore UnitTest-NetCore
	dotnet build /p:Configuration=Release UnitTest-NetCore
	msbuild /p:Configuration=Release $(PROJECT_NAME).sln

nupkg/CoCoL.$(VERSION).nupkg: $(PROJECT_NAME).nuspec
	nuget pack $(PROJECT_NAME).nuspec

pack: build nupkg/$(PROJECT_NAME).$(VERSION).nupkg

deploy: pack
	nuget push $(PROJECT_NAME).$(VERSION).nupkg -source "https://www.nuget.org"

clean:
	msbuild /t:Clean $(PROJECT_NAME).sln
	find . -type d -name obj | xargs rm -r
	find . -type d -name bin | xargs rm -r


.PHONY: all build clean deploy pack
