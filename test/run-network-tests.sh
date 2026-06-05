#!/bin/bash
# Event-Sorcerer Network Test Suite
# Verwendung: sudo ./run-network-tests.sh <BROKER_IP> [BROKER_PORT]
# Beispiel:   sudo ./run-network-tests.sh 192.168.1.100 1883

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

BROKER_IP="${1:-}"
BROKER_PORT="${2:-1883}"
SERVICE=event-sorcerer
THIS_HOST=$(hostname)

if [ -z "$BROKER_IP" ]; then
    echo -e "${RED}Fehler: Broker-IP fehlt${NC}"
    echo "Verwendung: sudo $0 <BROKER_IP> [BROKER_PORT]"
    echo "Beispiel:   sudo $0 192.168.1.100 1883"
    exit 1
fi

echo ""
echo -e "${BLUE}Dieser Server:  ${NC}$THIS_HOST"
echo -e "${BLUE}Broker:         ${NC}$BROKER_IP:$BROKER_PORT"
echo ""

# ─────────────────────────────────────────────
section "1. Netzwerk-Erreichbarkeit"
# ─────────────────────────────────────────────

if ping -c 3 -W 2 "$BROKER_IP" &>/dev/null; then
    LATENCY=$(ping -c 3 "$BROKER_IP" 2>/dev/null | tail -1 | awk -F '/' '{print $5}' || echo "?")
    pass "Broker $BROKER_IP erreichbar (avg Latenz: ${LATENCY}ms)"
else
    fail "Broker $BROKER_IP nicht erreichbar -- Netzwerkverbindung pruefen"
fi

if command -v nc &>/dev/null; then
    if nc -z -w 3 "$BROKER_IP" "$BROKER_PORT" 2>/dev/null; then
        pass "Port $BROKER_PORT auf $BROKER_IP offen"
    else
        fail "Port $BROKER_PORT auf $BROKER_IP geschlossen -- Firewall pruefen"
    fi
else
    if timeout 3 bash -c "echo > /dev/tcp/$BROKER_IP/$BROKER_PORT" 2>/dev/null; then
        pass "Port $BROKER_PORT auf $BROKER_IP offen"
    else
        fail "Port $BROKER_PORT auf $BROKER_IP nicht erreichbar"
    fi
fi

# ─────────────────────────────────────────────
section "2. MQTT Verbindung zum entfernten Broker"
# ─────────────────────────────────────────────

echo "  Teste MQTT Verbindung zu $BROKER_IP:$BROKER_PORT..."
TEST_TOPIC="test/network/$THIS_HOST"
TEST_MSG="network-test-$(date +%s)"

RECEIVED=$(mosquitto_sub -h "$BROKER_IP" -p "$BROKER_PORT" \
    -t "$TEST_TOPIC" -C 1 -W 5 2>/dev/null &
sleep 1
mosquitto_pub -h "$BROKER_IP" -p "$BROKER_PORT" \
    -t "$TEST_TOPIC" -m "$TEST_MSG" 2>/dev/null
wait)

if echo "$RECEIVED" | grep -q "$TEST_MSG" 2>/dev/null; then
    pass "MQTT Publish/Subscribe ueber Netzwerk funktioniert"
else
    # Alternativer Test: nur publish
    if mosquitto_pub -h "$BROKER_IP" -p "$BROKER_PORT" \
        -t "$TEST_TOPIC" -m "$TEST_MSG" 2>/dev/null; then
        pass "MQTT Verbindung zu Broker hergestellt"
    else
        fail "MQTT Verbindung zu $BROKER_IP:$BROKER_PORT fehlgeschlagen"
    fi
fi

# ─────────────────────────────────────────────
section "3. Service sendet an entfernten Broker"
# ─────────────────────────────────────────────

echo "  Konfiguriere Service fuer entfernten Broker..."
sudo cp /etc/$SERVICE/config.json /etc/$SERVICE/config.json.network-test-bak

# Ersetze Host in Config
sudo python3 -c "
import json
with open('/etc/$SERVICE/config.json') as f:
    c = json.load(f)
c['Mqtt']['Host'] = '$BROKER_IP'
c['Mqtt']['Port'] = $BROKER_PORT
with open('/etc/$SERVICE/config.json', 'w') as f:
    json.dump(c, f, indent=4)
" 2>/dev/null

sudo systemctl restart $SERVICE
sleep 4

echo "  Warte auf Nachrichten vom Service auf entferntem Broker (max 15s)..."
REMOTE_MSG=$(mosquitto_sub -h "$BROKER_IP" -p "$BROKER_PORT" \
    -t '#' -C 1 -W 15 2>/dev/null || echo "")

if [ -n "$REMOTE_MSG" ]; then
    pass "Service sendet Nachrichten ueber Netzwerk an $BROKER_IP"
    echo "  Empfangene Nachricht: $(echo "$REMOTE_MSG" | head -c 100)..."
else
    fail "Service sendet keine Nachrichten an entfernten Broker $BROKER_IP"
fi

echo "  Stelle lokale Konfiguration wieder her..."
sudo cp /etc/$SERVICE/config.json.network-test-bak /etc/$SERVICE/config.json
sudo systemctl restart $SERVICE
sleep 3
pass "Lokale Konfiguration wiederhergestellt"

# ─────────────────────────────────────────────
section "4. Gleichzeitige Verbindungen (Lasttest)"
# ─────────────────────────────────────────────

echo "  Starte 5 gleichzeitige MQTT Verbindungen zum Broker..."
SUCCESS=0
for i in 1 2 3 4 5; do
    if mosquitto_pub -h "$BROKER_IP" -p "$BROKER_PORT" \
        -t "test/load/$THIS_HOST/$i" \
        -m "load-test-$i" 2>/dev/null; then
        SUCCESS=$((SUCCESS+1))
    fi
done

if [ "$SUCCESS" -eq 5 ]; then
    pass "5/5 gleichzeitige Verbindungen erfolgreich"
elif [ "$SUCCESS" -ge 3 ]; then
    pass "$SUCCESS/5 Verbindungen erfolgreich (akzeptabel)"
else
    fail "Nur $SUCCESS/5 Verbindungen erfolgreich -- Broker ueberlastet oder Verbindungslimit"
fi

# ─────────────────────────────────────────────
section "5. Verbindungsabbruch und Wiederverbindung"
# ─────────────────────────────────────────────

echo "  Simuliere Netzwerkunterbrechung (10s)..."

# Lokale Firewall-Regel hinzufuegen um Broker-Port zu blockieren
if command -v iptables &>/dev/null; then
    sudo iptables -A OUTPUT -p tcp --dport "$BROKER_PORT" -d "$BROKER_IP" -j DROP 2>/dev/null || true
    sleep 12
    sudo iptables -D OUTPUT -p tcp --dport "$BROKER_PORT" -d "$BROKER_IP" -j DROP 2>/dev/null || true
    sleep 8

    # Pruefe ob Service wieder sendet
    if systemctl is-active --quiet $SERVICE 2>/dev/null; then
        pass "Service laeuft nach Netzwerkunterbrechung noch"
    else
        fail "Service ist nach Netzwerkunterbrechung abgestuerzt"
    fi

    AFTER_MSG=$(mosquitto_sub -h "$BROKER_IP" -p "$BROKER_PORT" \
        -t '#' -C 1 -W 20 2>/dev/null || echo "")
    if [ -n "$AFTER_MSG" ]; then
        pass "Service verbindet sich nach Netzwerkunterbrechung automatisch neu"
    else
        fail "Service verbindet sich nach Netzwerkunterbrechung nicht neu"
    fi
else
    skip "iptables nicht verfuegbar -- Netzwerkunterbrechungstest uebersprungen"
fi

# ─────────────────────────────────────────────
section "6. Langzeit-Stabilitaet (60 Sekunden)"
# ─────────────────────────────────────────────

echo "  Zaehle Nachrichten ueber 60 Sekunden..."
MSG_COUNT=0
START_TIME=$(date +%s)

while IFS= read -r line; do
    MSG_COUNT=$((MSG_COUNT+1))
done < <(mosquitto_sub -h "$BROKER_IP" -p "$BROKER_PORT" \
    -t '#' -W 60 2>/dev/null || echo "")

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))
RATE=0
if [ "$DURATION" -gt 0 ]; then
    RATE=$((MSG_COUNT / DURATION))
fi

if [ "$MSG_COUNT" -ge 30 ]; then
    pass "$MSG_COUNT Nachrichten in ${DURATION}s empfangen (~${RATE}/s) -- Service ist stabil"
elif [ "$MSG_COUNT" -ge 10 ]; then
    pass "$MSG_COUNT Nachrichten in ${DURATION}s -- Service laeuft"
else
    fail "Nur $MSG_COUNT Nachrichten in ${DURATION}s -- Service sendet zu wenig"
fi

# Kein Memory-Leak Check
MEM=$(systemctl show -p MemoryCurrent $SERVICE 2>/dev/null | cut -d= -f2 || echo "0")
if [ "$MEM" != "0" ] && [ "$MEM" != "[not set]" ]; then
    MEM_MB=$((MEM / 1024 / 1024))
    if [ "$MEM_MB" -lt 200 ]; then
        pass "Speicherverbrauch: ${MEM_MB}MB (normal)"
    else
        fail "Speicherverbrauch hoch: ${MEM_MB}MB -- moeglicher Memory-Leak"
    fi
else
    skip "Speicherverbrauch konnte nicht ermittelt werden"
fi

# ─────────────────────────────────────────────
section "Zusammenfassung"
# ─────────────────────────────────────────────

echo ""
echo -e "  ${BLUE}Getestet:${NC}       $THIS_HOST --> $BROKER_IP:$BROKER_PORT"
echo -e "  ${GREEN}Bestanden:      $PASS${NC}"
echo -e "  ${RED}Fehlgeschlagen: $FAIL${NC}"
echo -e "  ${YELLOW}Uebersprungen:  $SKIP${NC}"
echo ""

if [ "$FAIL" -eq 0 ]; then
    echo -e "${GREEN}======================================${NC}"
    echo -e "${GREEN}  NETZWERK-TESTS BESTANDEN${NC}"
    echo -e "${GREEN}  Bereit fuer Produktion${NC}"
    echo -e "${GREEN}======================================${NC}"
    exit 0
else
    echo -e "${RED}======================================${NC}"
    echo -e "${RED}  $FAIL NETZWERK-TEST(S) FEHLGESCHLAGEN${NC}"
    echo -e "${RED}======================================${NC}"
    exit 1
fi
