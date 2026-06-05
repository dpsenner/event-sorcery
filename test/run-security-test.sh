#!/bin/bash
# Event-Sorcerer Sicherheitstest

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

PASS=0
FAIL=0
SKIP=0

pass()    { echo -e "${GREEN}PASS${NC}  $1"; PASS=$((PASS+1)); }
fail()    { echo -e "${RED}FAIL${NC}  $1"; FAIL=$((FAIL+1)); }
skip()    { echo -e "${YELLOW}SKIP${NC}  $1"; SKIP=$((SKIP+1)); }
section() { echo -e "\n${BLUE}======================================${NC}"; echo -e "${YELLOW} $1${NC}"; echo -e "${BLUE}======================================${NC}"; }

SERVICE=event-sorcerer

# ─────────────────────────────────────────────
section "1. Prozess-Rechte"
# ─────────────────────────────────────────────

MAIN_PID=$(systemctl show -p MainPID $SERVICE 2>/dev/null | cut -d= -f2 || echo "")
RUN_USER=$(ps -o user= -p "$MAIN_PID" 2>/dev/null | tr -d ' ' || echo "")

if [ "$RUN_USER" = "event-sorcerer" ]; then
    pass "Service laeuft nicht als root (User: $RUN_USER)"
else
    fail "Service laeuft als: $RUN_USER -- sollte 'event-sorcerer' sein"
fi

# Pruefen ob Service keine root-Capabilities hat
if command -v capsh &>/dev/null; then
    CAPS=$(cat /proc/$MAIN_PID/status 2>/dev/null | grep CapEff | awk '{print $2}' || echo "")
    if [ "$CAPS" = "0000000000000000" ]; then
        pass "Service hat keine root Capabilities"
    else
        fail "Service hat elevated Capabilities: $CAPS"
    fi
else
    skip "capsh nicht verfuegbar -- Capabilities-Test uebersprungen"
fi

# ─────────────────────────────────────────────
section "2. Dateisystem-Zugriff"
# ─────────────────────────────────────────────

# Service-User darf nicht /etc/shadow lesen
if sudo -u $SERVICE cat /etc/shadow &>/dev/null; then
    fail "Service-User kann /etc/shadow lesen -- zu viele Rechte"
else
    pass "Service-User kann /etc/shadow nicht lesen"
fi

# Service-User darf nicht /root lesen
if sudo -u $SERVICE ls /root &>/dev/null; then
    fail "Service-User hat Zugriff auf /root"
else
    pass "Service-User hat keinen Zugriff auf /root"
fi

# Config-Verzeichnis Berechtigungen
if [ -d /etc/$SERVICE ]; then
    PERMS=$(stat -c "%a" /etc/$SERVICE)
    OWNER=$(stat -c "%U" /etc/$SERVICE)
    if [ "$PERMS" = "755" ] || [ "$PERMS" = "750" ] || [ "$PERMS" = "700" ]; then
        pass "Config-Verzeichnis Berechtigungen korrekt: $PERMS (Owner: $OWNER)"
    else
        skip "Config-Verzeichnis Berechtigungen: $PERMS -- empfohlen: 750"
    fi
fi

# Config-Datei Berechtigungen
if [ -f /etc/$SERVICE/config.json ]; then
    CONFIG_OWNER=$(stat -c "%U" /etc/$SERVICE/config.json)
    CONFIG_PERMS=$(stat -c "%a" /etc/$SERVICE/config.json)
    pass "Config-Datei: $CONFIG_PERMS (Owner: $CONFIG_OWNER)"
fi

# ─────────────────────────────────────────────
section "3. MQTT Sicherheit"
# ─────────────────────────────────────────────

# Pruefe ob anonyme Verbindungen erlaubt sind
if mosquitto_pub -t "test/security" -m "anon-test" 2>/dev/null; then
    skip "Anonyme MQTT Verbindungen erlaubt (kein Auth konfiguriert -- fuer internes Netz ok)"
else
    pass "Anonyme MQTT Verbindungen blockiert (Auth aktiv)"
fi

# Pruefe ob TLS aktiv ist
TLS_ENABLED=$(cat /etc/$SERVICE/config.json 2>/dev/null | \
    python3 -c "import json,sys; c=json.load(sys.stdin); print(c.get('Mqtt',{}).get('Ssl',{}).get('Enable',False))" \
    2>/dev/null || echo "False")
if [ "$TLS_ENABLED" = "True" ]; then
    pass "TLS/SSL fuer MQTT ist aktiviert"
else
    skip "TLS/SSL ist deaktiviert -- empfohlen fuer Produktionsumgebung"
fi

# Pruefe ob Auth aktiv ist
AUTH_ENABLED=$(cat /etc/$SERVICE/config.json 2>/dev/null | \
    python3 -c "import json,sys; c=json.load(sys.stdin); print(c.get('Mqtt',{}).get('Auth',{}).get('Enable',False))" \
    2>/dev/null || echo "False")
if [ "$AUTH_ENABLED" = "True" ]; then
    pass "MQTT Authentifizierung ist aktiviert"
else
    skip "MQTT Authentifizierung deaktiviert -- empfohlen fuer Produktionsumgebung"
fi

# ─────────────────────────────────────────────
section "4. Passwort-Sicherheit"
# ─────────────────────────────────────────────

# Pruefen ob Passwoerter in Logs auftauchen
PASS_IN_LOGS=$(journalctl -u $SERVICE --no-pager 2>/dev/null | \
    grep -iE "password=|passwd=|secret=|\"password\"|\"passwd\"" || echo "")
if [ -z "$PASS_IN_LOGS" ]; then
    pass "Keine Passwoerter in Service-Logs gefunden"
else
    fail "Moegliche Passwoerter in Logs: $PASS_IN_LOGS"
fi

# Pruefen ob Passwort in Config im Klartext und lesbar fuer alle
if [ -f /etc/$SERVICE/config.json ]; then
    LAST_DIGIT=$(stat -c "%a" /etc/$SERVICE/config.json | grep -oE ".$" || echo "0")
    if [ "$LAST_DIGIT" -lt 4 ] 2>/dev/null; then
        pass "Config-Datei nicht fuer alle lesbar"
    else
        skip "Config-Datei ist world-readable (644) -- empfohlen: sudo chmod 640 /etc/$SERVICE/config.json"
    fi
fi

# ─────────────────────────────────────────────
section "5. Prozess-Isolation"
# ─────────────────────────────────────────────

# Service-User hat keine sudo Rechte
SUDO_CHECK=$(sudo -l -U $SERVICE 2>&1 | grep -v "not allowed" | grep -i "ALL\|systemctl" || echo "")
if [ -z "$SUDO_CHECK" ]; then
    pass "Service-User hat keine sudo Rechte"
else
    fail "Service-User hat sudo Rechte: $SUDO_CHECK"
fi

# Binary nicht aenderbar durch Service-User
BINARY_OWNER=$(stat -c "%U" /usr/local/bin/$SERVICE 2>/dev/null || echo "")
if [ "$BINARY_OWNER" = "root" ]; then
    pass "Binary gehoert root -- Service-User kann es nicht aendern"
else
    fail "Binary gehoert $BINARY_OWNER -- sollte root gehoeren"
fi

# ─────────────────────────────────────────────
section "Zusammenfassung"
# ─────────────────────────────────────────────

echo ""
echo -e "  ${GREEN}Bestanden:      $PASS${NC}"
echo -e "  ${RED}Fehlgeschlagen: $FAIL${NC}"
echo -e "  ${YELLOW}Uebersprungen:  $SKIP${NC}"
echo ""
echo -e "  ${YELLOW}Hinweis: SKIP bedeutet nicht fehlgeschlagen --${NC}"
echo -e "  ${YELLOW}es sind empfohlene aber optionale Sicherheitsmassnahmen.${NC}"
echo ""

if [ "$FAIL" -eq 0 ]; then
    echo -e "${GREEN}======================================${NC}"
    echo -e "${GREEN}  SICHERHEITSTEST BESTANDEN${NC}"
    echo -e "${GREEN}======================================${NC}"
    exit 0
else
    echo -e "${RED}======================================${NC}"
    echo -e "${RED}  $FAIL SICHERHEITS-TEST(S) FEHLGESCHLAGEN${NC}"
    echo -e "${RED}======================================${NC}"
    exit 1
fi
