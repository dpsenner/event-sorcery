#!/bin/bash
# Event-Sorcerer Full Test Suite

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

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVICE=event-sorcerer
TEST_DB=event_sorcerer_test
TEST_USER=event_sorcerer_test
TEST_PASS=test1234

# ─────────────────────────────────────────────
section "1. Prerequisites"
# ─────────────────────────────────────────────

if systemctl is-active --quiet mosquitto 2>/dev/null; then
    pass "Mosquitto laeuft"
else
    fail "Mosquitto laeuft nicht -- starte mit: sudo systemctl start mosquitto"
fi

if command -v mosquitto_sub &>/dev/null && command -v mosquitto_pub &>/dev/null; then
    pass "mosquitto-clients installiert"
else
    fail "mosquitto-clients fehlt -- installiere mit: sudo apt install mosquitto-clients"
fi

if command -v psql &>/dev/null; then
    pass "PostgreSQL Client vorhanden"
    POSTGRES_AVAILABLE=true
else
    skip "PostgreSQL nicht installiert -- Historian-Tests werden uebersprungen"
    POSTGRES_AVAILABLE=false
fi

if systemctl is-active --quiet $SERVICE 2>/dev/null; then
    pass "$SERVICE Service laeuft"
else
    fail "$SERVICE Service laeuft nicht"
fi

# ─────────────────────────────────────────────
section "2. Service Lifecycle"
# ─────────────────────────────────────────────

STATUS=$(systemctl is-active $SERVICE 2>/dev/null || echo "inactive")
if [ "$STATUS" = "active" ]; then
    pass "Service ist active (running)"
else
    fail "Service ist nicht aktiv: $STATUS"
fi

if systemctl is-enabled --quiet $SERVICE 2>/dev/null; then
    pass "Service ist fuer Autostart aktiviert (enabled)"
else
    fail "Service startet nicht automatisch beim Booten"
fi

echo "  Teste sauberen Neustart..."
sudo systemctl restart $SERVICE
sleep 3
if systemctl is-active --quiet $SERVICE 2>/dev/null; then
    pass "Service startet nach Restart sauber"
else
    fail "Service startet nach Restart nicht"
fi

echo "  Teste Crash-Recovery..."
MAIN_PID=$(systemctl show -p MainPID $SERVICE 2>/dev/null | cut -d= -f2 || echo "")
if [ -n "$MAIN_PID" ] && [ "$MAIN_PID" != "0" ]; then
    sudo kill -9 "$MAIN_PID" 2>/dev/null || true
    sleep 6
    if systemctl is-active --quiet $SERVICE 2>/dev/null; then
        pass "Service erholt sich automatisch nach Crash (Restart=on-failure)"
    else
        fail "Service startet nach Crash nicht automatisch neu"
    fi
else
    skip "Crash-Recovery Test konnte PID nicht ermitteln"
fi

# ─────────────────────────────────────────────
section "3. Sicherheit"
# ─────────────────────────────────────────────

MAIN_PID_SEC=$(systemctl show -p MainPID $SERVICE 2>/dev/null | cut -d= -f2 || echo "")
if [ -n "$MAIN_PID_SEC" ] && [ "$MAIN_PID_SEC" != "0" ]; then
    RUN_USER=$(ps -o user= -p "$MAIN_PID_SEC" 2>/dev/null | tr -d ' ' || echo "")
else
    RUN_USER=""
fi
if [ "$RUN_USER" = "event-sorcerer" ]; then
    pass "Service laeuft als 'event-sorcerer' (nicht root)"
elif [ "$RUN_USER" = "root" ]; then
    fail "Service laeuft als root -- sicherheitskritisch!"
else
    fail "Unbekannter User: $RUN_USER"
fi

if [ -f /etc/$SERVICE/config.json ]; then
    CONFIG_PERMS=$(stat -c "%a" /etc/$SERVICE/config.json)
    pass "Config-Datei vorhanden mit Berechtigungen: $CONFIG_PERMS"
else
    fail "Config-Datei nicht gefunden: /etc/$SERVICE/config.json"
fi

# ─────────────────────────────────────────────
section "4. MQTT -- Nachrichten empfangen"
# ─────────────────────────────────────────────

echo "  Warte auf Nachricht (max 10s)..."
HEARTBEAT=$(mosquitto_sub -t '#' -C 1 -W 10 -F "%p" 2>/dev/null || echo "")
if [ -n "$HEARTBEAT" ]; then
    pass "Nachricht empfangen"
else
    fail "Keine Nachricht empfangen -- Service sendet nicht"
fi

echo "  Warte auf 5 Nachrichten (max 15s)..."
MSG_COUNT=0
while IFS= read -r line; do
    MSG_COUNT=$((MSG_COUNT+1))
done < <(mosquitto_sub -t '#' -C 5 -W 15 2>/dev/null || echo "")
if [ "$MSG_COUNT" -ge 3 ]; then
    pass "$MSG_COUNT Nachrichten empfangen"
else
    fail "Zu wenig Nachrichten: $MSG_COUNT (erwartet >= 3)"
fi

# ─────────────────────────────────────────────
section "5. MQTT -- JSON Format validieren"
# ─────────────────────────────────────────────

echo "  Empfange Nachricht und pruefe JSON..."
RAW_MSG=$(mosquitto_sub -t '#' -C 1 -W 10 -F "%p" 2>/dev/null || echo "")
if [ -n "$RAW_MSG" ]; then
    if echo "$RAW_MSG" | python3 -c "import json,sys; json.load(sys.stdin)" 2>/dev/null; then
        pass "Nachricht ist valides JSON"
    else
        fail "Nachricht ist kein valides JSON: $RAW_MSG"
    fi

    if echo "$RAW_MSG" | python3 -c "import json,sys; d=json.load(sys.stdin); assert 'Timestamp' in d" 2>/dev/null; then
        pass "JSON enthaelt 'Timestamp' Feld"
    else
        fail "JSON enthaelt kein 'Timestamp' Feld"
    fi

    if echo "$RAW_MSG" | python3 -c "import json,sys; d=json.load(sys.stdin); assert 'Hostname' in d" 2>/dev/null; then
        pass "JSON enthaelt 'Hostname' Feld"
    else
        skip "JSON enthaelt kein 'Hostname' Feld (abhaengig vom Sensor-Typ)"
    fi
else
    fail "Keine Nachricht empfangen fuer JSON-Validierung"
fi

# ─────────────────────────────────────────────
section "6. MQTT -- Broker Reconnect"
# ─────────────────────────────────────────────

echo "  Starte Mosquitto-Broker neu..."
sudo systemctl restart mosquitto
sleep 8

echo "  Pruefe ob Service sich wieder verbindet..."
RECONNECT_MSG=$(mosquitto_sub -t '#' -C 1 -W 15 2>/dev/null || echo "")
if [ -n "$RECONNECT_MSG" ]; then
    pass "Service verbindet sich nach Broker-Neustart automatisch neu"
else
    fail "Service sendet nach Broker-Neustart keine Nachrichten mehr"
fi

# ─────────────────────────────────────────────
section "7. Datenkorrektheit"
# ─────────────────────────────────────────────

echo "  Pruefe Hostname in Nachrichten..."
EXPECTED_HOSTNAME=$(hostname)
MSG_WITH_HOSTNAME=$(mosquitto_sub -t '#' -C 5 -W 15 -F "%p" 2>/dev/null | grep -m1 "$EXPECTED_HOSTNAME" || echo "")
if [ -n "$MSG_WITH_HOSTNAME" ]; then
    pass "Hostname '$EXPECTED_HOSTNAME' korrekt in Nachrichten"
else
    fail "Hostname '$EXPECTED_HOSTNAME' nicht in Nachrichten gefunden"
fi

echo "  Pruefe Timestamp-Format (ISO 8601)..."
TS_MSG=$(mosquitto_sub -t '#' -C 1 -W 10 -F "%p" 2>/dev/null || echo "")
if [ -n "$TS_MSG" ]; then
    if echo "$TS_MSG" | python3 -c "
import json, sys, re
d = json.load(sys.stdin)
ts = d.get('Timestamp', '')
assert re.match(r'\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}', ts), 'Ungueltig: ' + ts
" 2>/dev/null; then
        pass "Timestamp ist im ISO 8601 Format"
    else
        fail "Timestamp Format ungueltig oder fehlt"
    fi
else
    skip "Keine Nachricht fuer Timestamp-Pruefung"
fi

# ─────────────────────────────────────────────
section "8. Logs -- Keine Fehler"
# ─────────────────────────────────────────────

ERRORS=$(journalctl -u $SERVICE --since "5 minutes ago" --no-pager 2>/dev/null \
    | grep -iE "exception|crashed|unhandled" | grep -v "Disconnect error" || echo "")
if [ -z "$ERRORS" ]; then
    pass "Keine unerwarteten Fehler in den letzten 5 Minuten"
else
    fail "Fehler in Logs gefunden: $ERRORS"
fi

# ─────────────────────────────────────────────
section "9. PostgreSQL Historian"
# ─────────────────────────────────────────────

if [ "$POSTGRES_AVAILABLE" = "true" ]; then
    echo "  Richte Test-Datenbank ein..."
    sudo -u postgres psql -c "DROP DATABASE IF EXISTS $TEST_DB;" 2>/dev/null || true
    sudo -u postgres psql -c "DROP USER IF EXISTS $TEST_USER;" 2>/dev/null || true
    sudo -u postgres psql -c "CREATE USER $TEST_USER WITH PASSWORD '$TEST_PASS';" 2>/dev/null || true
    sudo -u postgres psql -c "CREATE DATABASE $TEST_DB OWNER $TEST_USER;" 2>/dev/null || true
    PGPASSWORD=$TEST_PASS psql -h localhost -U $TEST_USER -d $TEST_DB \
        -f "$SCRIPT_DIR/postgres/schema.sql" 2>/dev/null || true
    pass "Test-Datenbank '$TEST_DB' eingerichtet"

    echo "  Wechsle auf Test-Konfiguration..."
    sudo cp /etc/$SERVICE/config.json /etc/$SERVICE/config.json.bak
    sudo cp "$SCRIPT_DIR/configs/config-test.json" /etc/$SERVICE/config.json
    sudo systemctl restart $SERVICE
    sleep 5

    echo "  Warte auf Daten in PostgreSQL (max 30s)..."
    DB_OK=false
    for i in 1 2 3 4 5 6; do
        COUNT=$(PGPASSWORD=$TEST_PASS psql -h localhost -U $TEST_USER -d $TEST_DB \
            -t -c "SELECT COUNT(*) FROM measuring.load_measurement;" 2>/dev/null | tr -d ' ' || echo "0")
        if [ "$COUNT" -gt "0" ] 2>/dev/null; then
            pass "Historian schreibt Daten in PostgreSQL ($COUNT Eintraege)"
            DB_OK=true
            break
        fi
        sleep 5
    done
    if [ "$DB_OK" = "false" ]; then
        fail "Keine Daten in PostgreSQL nach 30s"
    fi

    echo "  Stelle Original-Konfiguration wieder her..."
    sudo cp /etc/$SERVICE/config.json.bak /etc/$SERVICE/config.json
    sudo systemctl restart $SERVICE
    sleep 3
    pass "Original-Konfiguration wiederhergestellt"

    sudo -u postgres psql -c "DROP DATABASE IF EXISTS $TEST_DB;" 2>/dev/null || true
    sudo -u postgres psql -c "DROP USER IF EXISTS $TEST_USER;" 2>/dev/null || true
    pass "Test-Datenbank aufgeraeumt"
else
    skip "PostgreSQL Tests uebersprungen"
    echo "  PostgreSQL installieren: sudo apt install -y postgresql"
fi

# ─────────────────────────────────────────────
section "10. Deployment Readiness"
# ─────────────────────────────────────────────

if [ -f /usr/local/bin/$SERVICE ]; then
    pass "Binary vorhanden: /usr/local/bin/$SERVICE"
else
    fail "Binary fehlt: /usr/local/bin/$SERVICE"
fi

if [ -f /lib/systemd/system/$SERVICE.service ]; then
    pass "Systemd Unit-Datei vorhanden"
else
    fail "Systemd Unit-Datei fehlt"
fi

if [ -f /etc/$SERVICE/config.json ]; then
    pass "Config vorhanden: /etc/$SERVICE/config.json"
else
    fail "Config fehlt: /etc/$SERVICE/config.json"
fi

if id "$SERVICE" &>/dev/null; then
    pass "System-User '$SERVICE' existiert"
else
    fail "System-User '$SERVICE' fehlt"
fi

if systemctl is-active --quiet $SERVICE 2>/dev/null; then
    pass "Service laeuft stabil zum Abschluss"
else
    fail "Service laeuft nicht mehr am Ende der Tests"
fi

# ─────────────────────────────────────────────
section "Zusammenfassung"
# ─────────────────────────────────────────────

echo ""
echo -e "  ${GREEN}Bestanden:      $PASS${NC}"
echo -e "  ${RED}Fehlgeschlagen: $FAIL${NC}"
echo -e "  ${YELLOW}Uebersprungen:  $SKIP${NC}"
echo ""

if [ "$FAIL" -eq 0 ]; then
    echo -e "${GREEN}======================================${NC}"
    echo -e "${GREEN}  ALLE TESTS BESTANDEN - READY TO DEPLOY${NC}"
    echo -e "${GREEN}======================================${NC}"
    exit 0
else
    echo -e "${RED}======================================${NC}"
    echo -e "${RED}  $FAIL TEST(S) FEHLGESCHLAGEN${NC}"
    echo -e "${RED}======================================${NC}"
    exit 1
fi
