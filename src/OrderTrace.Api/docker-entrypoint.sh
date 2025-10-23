#!/bin/bash
set -e

echo "=========================================="
echo "  OrderTrace API - Starting..."
echo "=========================================="

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL..."
until dotnet ef database update --no-build 2>/dev/null; do
  echo "PostgreSQL is unavailable - retrying in 5 seconds..."
  sleep 5
done

echo "Database migrations applied successfully"
echo ""
echo "Starting API..."
exec dotnet OrderTrace.Api.dll
