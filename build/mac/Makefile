ROOT = $(realpath ../../)
CUR_DIR = $(realpath .)

7ZIP_SUBMODULE_DIR = $(ROOT)/submodules/7-Zip
CURL_IMPERSONATE_SUBMODULE_DIR = $(ROOT)/submodules/curl-impersonate

BUILD_DIR = $(CUR_DIR)/build
CURL_BUILD_DIR = $(BUILD_DIR)/curl-impersonate
7Z_BUILD_DIR = $(7ZIP_SUBMODULE_DIR)/CPP/7zip/Bundles/Alone2
STALKER_GAMMA_BUILD_DIR = $(BUILD_DIR)/stalker-gamma-cli
STALKER_GAMMA_CLI_PROJECT = $(ROOT)/stalker-gamma.cli/stalker-gamma.cli.csproj
STALKER_RESOURCES_DIR = $(STALKER_GAMMA_BUILD_DIR)/resources
ARTIFACT_OUTPUT_DIR ?= $(STALKER_GAMMA_BUILD_DIR)

build:
	mkdir $(BUILD_DIR)

	# build curl-impersonate
	mkdir $(CURL_BUILD_DIR)
	(cd $(CURL_BUILD_DIR) && $(CURL_IMPERSONATE_SUBMODULE_DIR)/configure --enable-static --prefix=$(CURL_BUILD_DIR)/install LDFLAGS="-L$(brew --prefix zstd)/lib" CPPFLAGS="-I$(brew --prefix zstd)/include" && gmake build && gmake install)

	# build 7zip
	$(MAKE) -C $(7Z_BUILD_DIR) -f makefile.gcc
	
	# build stalker-gamma-cli
	mkdir $(STALKER_GAMMA_BUILD_DIR)
	dotnet publish -c Release $(STALKER_GAMMA_CLI_PROJECT) -o $(ARTIFACT_OUTPUT_DIR)

	# cleanup stalker-gamma-cli debug files
	rm $(ARTIFACT_OUTPUT_DIR)/*.pdb
	rm -rf $(ARTIFACT_OUTPUT_DIR)/*.dSYM

	# copy dependency artifacts to stalker-gamma-cli
	mkdir $(STALKER_RESOURCES_DIR)
	cp $(CURL_BUILD_DIR)/install/bin/curl-impersonate $(STALKER_RESOURCES_DIR)/
	cp $(7Z_BUILD_DIR)/_o/7zz $(STALKER_RESOURCES_DIR)/
	cp install.sh $(ARTIFACT_OUTPUT_DIR)/
	cp uninstall.sh $(ARTIFACT_OUTPUT_DIR)/

archive:
	tar -cvJf stalker-gamma-cli.tar.xz --directory=$(ARTIFACT_OUTPUT_DIR) .

install:
	$(STALKER_GAMMA_BUILD_DIR)/install.sh
	
clean:
	rm -rf $(BUILD_DIR)
	$(MAKE) -C $(7Z_BUILD_DIR) -f makefile.gcc clean

uninstall:
	./uninstall.sh