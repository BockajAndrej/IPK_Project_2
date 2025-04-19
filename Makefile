# Makefile
PROJECT = ./IPK25-CHAT/IPK25-CHAT.csproj
EXECUTABLE=IPK25-CHAT

all: clean publish

# Self-contained build (nezávislý od system .NET runtime)
publish:
	dotnet publish $(PROJECT) -r linux-x64 -c Release -o ./publish

# Vyčistenie projektu (binárky aj výstup)
clean:
	rm -rf bin obj publish
