patch-makefile:
	patch --directory=curl-impersonate < curl-impersonate/patches/makefile.patch

build:
	mkdir curl-impersonate/build
	(cd curl-impersonate/build && ../configure --enable-static --prefix="$(shell pwd)/curl-impersonate/build/install" && make build && make install)

	$(MAKE) -C 7-Zip/CPP/7zip/Bundles/Alone2 -f makefile.gcc
	dotnet publish -c Release stalker-gamma.cli/stalker-gamma.cli.csproj -o bin

	cp curl-impersonate/build/install/bin/curl-impersonate bin/resources/curl-impersonate/linux/
	cp 7-Zip/CPP/7zip/Bundles/Alone2/_o/7zz bin/resources/7zip/linux/

install:
	mkdir $(HOME)/.local/bin/stalker-gamma.cli
	cp -R bin/* $(HOME)/.local/bin/stalker-gamma.cli
	ln -s $(HOME)/.local/bin/stalker-gamma.cli/stalker-gamma.cli $(HOME)/.local/bin/stalker-gamma-cli 
	
clean:
	rm -rf ./curl-impersonate/build
	rm -rf ./-C 7-Zip/CPP/7zip/Bundles/Alone2/_o
	rm -rf ./bin

uninstall:
	rm -rf $(HOME)/.local/bin/stalker-gamma.cli
	rm $(HOME)/.local/bin/stalker-gamma-cli