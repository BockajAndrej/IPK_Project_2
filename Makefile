# Makefile
PROJECT_PATH = ./IPK25-CHAT/IPK25-CHAT.csproj
CONFIGURATION = Release
RUNTIME = linux-x64
OUTPUT_DIR = ./
EXECUTABLE = ipk25chat-client

.PHONY: all clean publish

all: publish

publish:
	dotnet publish $(PROJECT_PATH) \
		-c $(CONFIGURATION) \
		-r $(RUNTIME) \
		-p:AssemblyName=$(EXECUTABLE) \
		-o $(OUTPUT_DIR)

clean:
	dotnet clean $(PROJECT_PATH)
	rm -rf $(OUTPUT_DIR)