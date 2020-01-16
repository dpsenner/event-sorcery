OUTPUT_DIR=build
CSPROJ_FILE=src/csharp/EventSorcerer/EventSorcerer.csproj
CONFIGURATION_PATH=src/deploy

.DEFAULT_GOAL := build
run:
	# run
	-dotnet run --project $(CSPROJ_FILE) -- -c $(CONFIGURATION_PATH)
run-with-clean-session:
	# run-with-clean-session
	-dotnet run --project $(CSPROJ_FILE) -- -c $(CONFIGURATION_PATH) --clean-session
clean:
	# clean
	test ! -d $(OUTPUT_DIR) || rm -rf $(OUTPUT_DIR)
	-rm -rf src/csharp/*/bin/ src/csharp/*/obj/
build: build-portable build-linux-x64 build-linux-arm build-deploy
build-deploy:
	# build-deploy
	test ! -f $(OUTPUT_DIR)/deploy || rm -rf $(OUTPUT_DIR)/deploy
	mkdir -p $(OUTPUT_DIR)/deploy
	cp -r src/deploy $(OUTPUT_DIR)
	-rm $(OUTPUT_DIR)/deploy/conf.d/*.json
build-portable:
	# build-portable
	test ! -f $(OUTPUT_DIR)/portable || rm -rf $(OUTPUT_DIR)/portable
	dotnet publish --configuration Release --output $(OUTPUT_DIR)/portable $(CSPROJ_FILE)
	chmod -R -x,+X $(OUTPUT_DIR)/portable/*
	cp src/deploy/event-sorcerer.sh $(OUTPUT_DIR)/portable/EventSorcerer
	chmod +x $(OUTPUT_DIR)/portable/EventSorcerer
build-linux-x64:
	# build-linux-x64
	test ! -f $(OUTPUT_DIR)/linux-x64 || rm -rf $(OUTPUT_DIR)/linux-x64
	dotnet publish --configuration Release --self-contained --runtime linux-x64 --output $(OUTPUT_DIR)/linux-x64 $(CSPROJ_FILE)
	chmod -R -x,+X $(OUTPUT_DIR)/linux-x64/*
	chmod +x $(OUTPUT_DIR)/linux-x64/EventSorcerer
build-linux-arm:
	# build-linux-arm
	test ! -f $(OUTPUT_DIR)/linux-arm || rm -rf $(OUTPUT_DIR)/linux-arm
	dotnet publish --configuration Release --self-contained --runtime linux-arm --output $(OUTPUT_DIR)/linux-arm $(CSPROJ_FILE)
	chmod -R -x,+X $(OUTPUT_DIR)/linux-arm/*
	chmod +x $(OUTPUT_DIR)/linux-arm/EventSorcerer

deploy-install:
	# deploy-install
	scp -r build $(HOST):/tmp/event-sorcerer
	ssh $(HOST) sudo make -C /tmp/event-sorcerer/deploy install ARCH=$(ARCH)
	ssh $(HOST) rm -r /tmp/event-sorcerer
deploy-upgrade:
	# deploy-upgrade
	# copy binaries
	ssh $(HOST) mkdir -p /tmp/event-sorcerer
	scp -r build/deploy $(HOST):/tmp/event-sorcerer/deploy
	scp -r build/$(ARCH) $(HOST):/tmp/event-sorcerer/$(ARCH)
	# install
	ssh $(HOST) sudo make -C /tmp/event-sorcerer/deploy install-bin install-etc ARCH=$(ARCH)
	# clean up
	ssh $(HOST) rm -r /tmp/event-sorcerer
