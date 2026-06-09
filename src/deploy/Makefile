APP_NAME=event-sorcerer

install: install-user install-etc install-bin install-systemd-unit
install-user:
	id -u $(APP_NAME) &>/dev/null || useradd --system --no-create-home --shell /usr/sbin/nologin $(APP_NAME)
install-etc:
	mkdir -p /etc/$(APP_NAME)/conf.d
	cp config.json /etc/$(APP_NAME)/config.json
	cp -r conf.d /etc/$(APP_NAME)/
install-bin:
	# this file must exist
	test -f ../$(ARCH)/EventSorcerer
	# force remove existing binaries
	-rm -rf /var/opt/$(APP_NAME)
	mkdir -p /var/opt/$(APP_NAME)
	cp -r ../$(ARCH)/ /var/opt/$(APP_NAME)/
	# symlink executable
	ln -sf /var/opt/$(APP_NAME)/$(ARCH)/EventSorcerer /usr/local/bin/event-sorcerer
install-systemd-unit:
	cp $(APP_NAME).service /lib/systemd/system/$(APP_NAME).service
	systemctl daemon-reload
	systemctl enable $(APP_NAME)
status:
	systemctl status $(APP_NAME)
start:
	systemctl start $(APP_NAME)
stop:
	systemctl stop $(APP_NAME)
restart:
	systemctl restart $(APP_NAME)

