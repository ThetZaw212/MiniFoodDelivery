#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

if [[ ! -f .env ]]; then
  echo "Copy .env.example to .env and set SQL_SA_PASSWORD first."
  exit 1
fi

dotnet run --project src/Services/Catalog/Catalog.Api --launch-profile http &
dotnet run --project src/Services/Ordering/Ordering.Api --launch-profile http &
dotnet run --project src/Services/Delivery/Delivery.Api --launch-profile http &
dotnet run --project src/Services/Notification/Notification.Api --launch-profile http &
dotnet run --project src/Gateway/Gateway.Api --launch-profile http &

echo "Gateway http://localhost:5080  Catalog Scalar http://localhost:5081/scalar"
wait
