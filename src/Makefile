all: build

PROJECT_NAME=CoCoL
DEPENDS_CS=$(shell find . -type f -name *.cs | xargs echo)
VERSION=$(shell cat $(PROJECT_NAME).nuspec | grep "<version>" | tr -d "[A-z/<>] ")

build: $(PROJECT_NAME).sln $(DEPENDS_CS)
	msbuild /p:Configuration=Release $(PROJECT_NAME).sln

nupkg/CoCoL.$(VERSION).nupkg: $(PROJECT_NAME).nuspec
	nuget pack $(PROJECT_NAME).nuspec

pack: nupkg/$(PROJECT_NAME).$(VERSION).nupkg build

deploy: pack
	nuget push $(PROJECT_NAME).$(VERSION).nupkg

clean:
	msbuild /t:Clean $(PROJECT_NAME).sln
	find . -type d -name obj | xargs rm -r
	find . -type d -name bin | xargs rm -r


.PHONY: all build clean deploy pack
