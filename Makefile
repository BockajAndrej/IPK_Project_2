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
	rm -f $(OUTPUT_DIR)/$(EXECUTABLE)
	rm -f $(OUTPUT_DIR)/*.dll
	rm -f $(OUTPUT_DIR)/*.pdb
	rm -f $(OUTPUT_DIR)/*.runtimeconfig.json
	rm -f $(OUTPUT_DIR)/*.deps.json