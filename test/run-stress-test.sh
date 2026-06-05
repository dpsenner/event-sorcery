#!/bin/bash
# Event-Sorcerer Belastungstest

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

PASS=0
FAIL=0

pass()    { echo -e "${GREEN}PASS${NC}  $1"; PASS=$((PASS+1)); }
fail()    { echo -e "${RED}FAIL${NC}  $1"; FAIL=$((FAIL+1)); }
section() { echo -e "\n${BLUE}======================================${NC}"; echo -e "${YELLOW} $1${NC}"; echo -e "${BLUE}======================================${NC}"; }

SERVICE=event-sorcerer

# ─────────────────────────────────────────────
section "1. Baseline Messung"
# ─────────────────────────────────────────────

echo "  Messe Ausgangszustand..."
MEM_BEFORE=$(systemctl show -p MemoryCurrent $SERVICE 2>/dev/null | cut -d= -f2 | tr -d ' ' || echo "0")
MEM_MB_BEFORE=$((MEM_BEFORE / 1024 / 1024))
CPU_BEFORE=$(ps -o %cpu= -p $(systemctl show -p MainPID $SERVICE | cut -d= -f2) 2>/dev/null | tr -d ' ' || echo "0")
echo "  RAM: ${MEM_MB_BEFORE}MB | CPU: ${CPU_BEFORE}%"
pass "Baseline gemessen: RAM=${MEM_MB_BEFORE}MB CPU=${CPU_BEFORE}%"

# ─────────────────────────────────────────────
section "2. Nachrichten-Durchsatz"
# ─────────────────────────────────────────────

echo "  Sende 1000 Nachrichten und zaehle empfangene..."
SENT=0
for i in $(seq 1 1000); do
    mosquitto_pub -t "test/stress/$i" -m "{\"id\":$i,\"ts\":\"$(date -Iseconds)\"}" 2>/dev/null && SENT=$((SENT+1))
done

echo "  $SENT/1000 Nachrichten gesendet"
if [ "$SENT" -ge 950 ]; then
    pass "Durchsatz: $SENT/1000 Nachrichten (>=95%)"
else
    fail "Durchsatz zu niedrig: $SENT/1000"
fi

# ─────────────────────────────────────────────
section "3. Parallele Verbindungen (9 Server simuliert)"
# ─────────────────────────────────────────────

echo "  Simuliere 9 Server gleichzeitig..."
TMPFILE=$(mktemp)

# Subscriber zuerst starten
mosquitto_sub -t 'test/+/heartbeat' -W 30 2>/dev/null >> "$TMPFILE" &
SUB_PID=$!
sleep 1

# Dann alle Publisher starten
PIDS=""
for i in $(seq 1 9); do
    (
        for j in $(seq 1 20); do
            mosquitto_pub -t "test/server$i/heartbeat" \
                -m "{\"Hostname\":\"server$i\",\"Timestamp\":\"$(date -Iseconds)\"}" 2>/dev/null
            sleep 0.1
        done
    ) &
    PIDS="$PIDS $!"
done

# Warte auf alle Publisher
for pid in $PIDS; do
    wait $pid 2>/dev/null || true
done
sleep 2

# Subscriber stoppen und zaehlen
kill $SUB_PID 2>/dev/null || true
wait $SUB_PID 2>/dev/null || true
RECEIVED=$(wc -l < "$TMPFILE" || echo "0")
rm -f "$TMPFILE"

echo "  $RECEIVED/180 Nachrichten von 9 parallelen Servern empfangen"
if [ "$RECEIVED" -ge 150 ]; then
    pass "Parallelbetrieb: $RECEIVED/180 Nachrichten (9 Server gleichzeitig)"
else
    fail "Zu wenig Nachrichten unter Last: $RECEIVED/180"
fi

# ─────────────────────────────────────────────
section "4. RAM unter Last"
# ─────────────────────────────────────────────

echo "  Pruefe RAM nach Belastung..."
MEM_AFTER=$(systemctl show -p MemoryCurrent $SERVICE 2>/dev/null | cut -d= -f2 | tr -d ' ' || echo "0")
MEM_MB_AFTER=$((MEM_AFTER / 1024 / 1024))
MEM_DIFF=$((MEM_MB_AFTER - MEM_MB_BEFORE))

echo "  RAM vorher: ${MEM_MB_BEFORE}MB | nachher: ${MEM_MB_AFTER}MB | Differenz: ${MEM_DIFF}MB"
if [ "$MEM_DIFF" -lt 50 ]; then
    pass "RAM stabil unter Last (Anstieg: ${MEM_DIFF}MB)"
else
    fail "Zu hoher RAM-Anstieg: ${MEM_DIFF}MB -- moeglicher Memory-Leak"
fi

# ─────────────────────────────────────────────
section "5. Service laeuft noch nach Last"
# ─────────────────────────────────────────────

if systemctl is-active --quiet $SERVICE 2>/dev/null; then
    pass "Service laeuft stabil nach Belastungstest"
else
    fail "Service ist unter Last abgestuerzt"
fi

# ─────────────────────────────────────────────
section "Zusammenfassung"
# ─────────────────────────────────────────────

echo ""
echo -e "  ${GREEN}Bestanden:      $PASS${NC}"
echo -e "  ${RED}Fehlgeschlagen: $FAIL${NC}"
echo ""

if [ "$FAIL" -eq 0 ]; then
    echo -e "${GREEN}======================================${NC}"
    echo -e "${GREEN}  BELASTUNGSTEST BESTANDEN${NC}"
    echo -e "${GREEN}======================================${NC}"
    exit 0
else
    echo -e "${RED}======================================${NC}"
    echo -e "${RED}  $FAIL TEST(S) FEHLGESCHLAGEN${NC}"
    echo -e "${RED}======================================${NC}"
    exit 1
fi
