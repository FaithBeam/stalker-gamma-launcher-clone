patch-makefile:
	patch --directory=curl-impersonate < curl-impersonate/patches/makefile.patch

build:
	# build curl-impersonate
	mkdir curl-impersonate/build
	(cd curl-impersonate/build && ../configure --enable-static --prefix="$(shell pwd)/curl-impersonate/build/install" && make build && make install)

	# build 7zip
	$(MAKE) -C 7-Zip/CPP/7zip/Bundles/Alone2 -f makefile.gcc
	
	# build stalker-gamma-cli
	dotnet publish -c Release stalker-gamma.cli/stalker-gamma.cli.csproj -o bin

	# cleanup stalker-gamma-cli debug files
	rm ./bin/*.pdb
	rm ./bin/*.dbg

	# copy dependency artifacts to stalker-gamma-cli
	cp curl-impersonate/build/install/bin/curl-impersonate bin/resources/curl-impersonate/linux/
	cp 7-Zip/CPP/7zip/Bundles/Alone2/_o/7zz bin/resources/7zip/linux/
	cp install.sh bin/
	cp uninstall.sh bin/

archive:
	ZSTD_CLEVEL=22 tar --zstd -cvf stalker-gamma-cli.tar.zst --directory=./bin .

install:
	./bin/install.sh
	
clean:
	rm -rf ./curl-impersonate/build
	rm -rf ./-C 7-Zip/CPP/7zip/Bundles/Alone2/_o
	rm -rf ./bin

uninstall:
	./uninstall.sh