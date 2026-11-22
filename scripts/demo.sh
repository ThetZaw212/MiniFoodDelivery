#!/usr/bin/env bash
set -euo pipefail

gateway="${GATEWAY:-http://localhost:5080}"
restaurant="11111111-1111-1111-1111-111111111111"
mohinga="11111111-1111-1111-1111-111111111102"
salad="11111111-1111-1111-1111-111111111101"

echo "1. Load balancer: two restaurant calls"
curl -sD - -o /dev/null "$gateway/catalog/api/restaurants" | awk 'BEGIN{IGNORECASE=1} /^HTTP|^X-Service-Instance|^X-Cache|^X-Gateway/'
echo "---"
curl -sD - -o /dev/null "$gateway/catalog/api/restaurants" | awk 'BEGIN{IGNORECASE=1} /^HTTP|^X-Service-Instance|^X-Cache|^X-Gateway/'

echo
echo "2. Place an order (sync gRPC to Catalog, then async Redis)"
order="$(curl -sf -X POST "$gateway/orders/api/orders" \
  -H 'Content-Type: application/json' \
  -d "{\"restaurantId\":\"$restaurant\",\"customerName\":\"Thet\",\"deliveryAddress\":\"12 Strand Road, Yangon\",\"lines\":[{\"menuItemId\":\"$mohinga\",\"quantity\":2},{\"menuItemId\":\"$salad\",\"quantity\":1}]}")"
echo "$order"
order_id="$(printf '%s' "$order" | sed -n 's/.*"id":"\([^"]*\)".*/\1/p')"

echo
echo "Waiting for the async delivery worker..."
sleep 1

echo
echo "3. Read the order again (sync gRPC to Delivery)"
curl -sf "$gateway/orders/api/orders/$order_id"
echo

echo
echo "4. Deliveries"
curl -sf "$gateway/delivery/api/deliveries"
echo

echo
echo "5. Notifications"
curl -sf "$gateway/notifications/api/notifications"
echo
