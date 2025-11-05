#!/bin/bash
set -e

echo "=========================================="
echo "  OrderTrace API - Starting..."
echo "=========================================="

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL..."
until PGPASSWORD=$DB_PASSWORD psql -h "${DB_HOST:-postgres}" -U "${DB_USER:-postgres}" -d "${DB_NAME:-ordertrace}" -c '\q' 2>/dev/null; do
  echo "PostgreSQL is unavailable - retrying in 3 seconds..."
  sleep 3
done

echo "PostgreSQL is ready!"
echo ""
echo "Starting API (migrations will run automatically)..."
exec dotnet OrderTrace.Api.dll
